using MassTransit;
using MttApplication.Contracts;
using MttApplication.Entities;

namespace MttApplication.Sagas;

public class GameState: SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string? CurrentState { get; set; }
    public int? CurrentQuestionIndex { get; set; } = null;
    public IList<Participant> Participants { get; set; } = new List<Participant>();
    public IList<Question> Questions { get; set; } = new List<Question>();
    public IList<QuestionResponse> Responses { get; set; } = new List<QuestionResponse>();
    public IList<QuestionResponseScore> Scores { get; set; } = new List<QuestionResponseScore>();

    
    
    public int AnswerTimeSeconds { get; set; } = 10;
    public int ShowResultTimeSeconds { get; set; } = 5;
    public Guid? CloseCurrentQuestionSchedulerTokenId { get; set; }
    public Guid? NextQuestionSchedulerTokenId { get; set; }
    public bool HasAnotherQuestion =>
        CurrentQuestionIndex != null && Questions.Any(q => q.QuestionIndex == CurrentQuestionIndex + 1);
    
}

public class GameStateMachine : MassTransitStateMachine<GameState>
{
    public GameStateMachine()
    {
        InstanceState(x => x.CurrentState);
        Request(() => FetchQuestionsRequest);
        Schedule(() => CloseCurrentQuestionScheduler, x => x.CloseCurrentQuestionSchedulerTokenId);
        Schedule(() => NextQuestionScheduler, x => x.NextQuestionSchedulerTokenId);
        
        Initially(When(CreateGame)
            .TransitionTo(LobbyOpen));
        
        During(LobbyOpen,
            When(AddParticipant)
                .Then(ctx => ctx.Saga.Participants.Add(ctx.Message.Participant!)),
            When(StartGame)
                .Request(FetchQuestionsRequest, ctx => 
                new FetchQuestions(){ CorrelationId = ctx.CorrelationId!.Value})
                .TransitionTo(FetchQuestionsRequest!.Pending)
        );
        
        During(FetchQuestionsRequest.Pending,
            When(FetchQuestionsRequest.Completed)
                .Then(ctx =>
                {
                    ctx.Saga.Questions = ctx.Message.Questions;
                    ctx.Saga.CurrentQuestionIndex = 0;
                    ctx.Saga.Questions[ctx.Saga.CurrentQuestionIndex.Value].QuestionOpened = DateTime.Now;
                })
                .Schedule(CloseCurrentQuestionScheduler, 
                    ctx => ctx.Init<CloseCurrentQuestion>(new CloseCurrentQuestion(){CorrelationId = ctx.Saga.CorrelationId}),
                    ctx => TimeSpan.FromSeconds(ctx.Saga.AnswerTimeSeconds))
                .TransitionTo(QuestionOpen)
        );
        
        During(QuestionOpen,
            When(AnswerQuestion)
                .Then(ctx =>
                {
                    var answeredPreviously = ctx.Saga.Responses.Any(r =>
                        r.ParticipantId == ctx.Message.ParticipantId && r.QuestionIndex == ctx.Message.QuestionIndex);
                    if (!answeredPreviously) ctx.Saga.Responses.Add(ctx.Message);
                }),
            When(CloseCurrentQuestion)
                .Then(ctx =>
                {
                    ctx.Saga.Questions[ctx.Saga.CurrentQuestionIndex!.Value].QuestionClosed = DateTime.UtcNow;
                    CalculateScores(ctx.Saga);
                })
                .Schedule(NextQuestionScheduler,
                    ctx => ctx.Init<NextQuestion>(new NextQuestion(){CorrelationId = ctx.Saga.CorrelationId}),
                    ctx => TimeSpan.FromSeconds(ctx.Saga.ShowResultTimeSeconds))
                .TransitionTo(QuestionResult)
        );
        
        
        During(QuestionResult,
            // order of these when activities is important
            When(NextQuestion, ctx => !ctx.Saga.HasAnotherQuestion)
                .Then(ctx =>
                {
                    Console.WriteLine("Finishing!");
                })
                .TransitionTo(Final),
            When(NextQuestion, ctx => ctx.Saga.HasAnotherQuestion)
                .TransitionTo(QuestionOpen)
                .Then(ctx =>
                {
                    ctx.Saga.CurrentQuestionIndex++;
                    ctx.Saga.Questions[ctx.Saga.CurrentQuestionIndex!.Value].QuestionOpened = DateTime.UtcNow;
                })
                
            );
        
        DuringAny(When(GetParticipantState)
            .Respond(ctx => new ParticipantStateResponse()
            {
                CorrelationId = ctx.Saga.CorrelationId,
                CurrentState = ctx.Saga.CurrentState,
                CurrentQuestionIndex = ctx.Saga.CurrentQuestionIndex,
                CurrentQuestion = ctx.Saga.CurrentQuestionIndex != null 
                    ? new ParticipantQuestion(ctx.Saga.Questions[ctx.Saga.CurrentQuestionIndex.Value]) 
                    : null,
                QuestionResponses = ctx.Saga.Responses.Where(r => r.ParticipantId == ctx.Message.ParticipantId).ToList(),
                QuestionResponseScores = ctx.Saga.Scores.Where(r => r.ParticipantId == ctx.Message.ParticipantId).ToList(),
            }));
        
    } // end ctor

    private void CalculateScores(GameState gameState)
    {
        var question = gameState.Questions[gameState.CurrentQuestionIndex!.Value];
        var questionOpenTimespan = question.QuestionOpened!.Value.Subtract(question.QuestionClosed!.Value);
        foreach (var participant in gameState.Participants)
        {
            var participantAnswer = gameState.Responses.FirstOrDefault(r =>
                r.ParticipantId == participant.ParticipantId 
                && r.QuestionIndex == question.QuestionIndex 
                && r.Timestamp >= question.QuestionOpened 
                && r.Timestamp < question.QuestionClosed);
            if (participantAnswer == null) continue;

            var answerTimespan = participantAnswer.Timestamp.Subtract(question.QuestionOpened.Value);
            int points = 0;
            if (participantAnswer.Answer == question.CorrectAnswer)
            {
                // apply point based on answer time
                points = 4 - (int)Math.Floor(answerTimespan.Divide(questionOpenTimespan.Divide(4)));
            }
            else
            {
                // subtract points based on answer time
                points = - 2 + (int)Math.Floor(answerTimespan.Divide(questionOpenTimespan.Divide(2)));
            }
            
            gameState.Scores.Add(new QuestionResponseScore()
            {
                CorrelationId = gameState.CorrelationId,
                ParticipantId = participant.ParticipantId,
                QuestionIndex = participantAnswer.QuestionIndex,
                Score = points
            });
        }
    }

    public State LobbyOpen { get; private set; }
    public State QuestionOpen { get; private set; }
    public State QuestionResult { get; private set; }
    
    
    public Event<CreateGame> CreateGame { get; set; }
    public Event<AddParticipant> AddParticipant { get; set; }
    public Event<StartGame> StartGame { get; set; }
    public Request<GameState, FetchQuestions, QuestionsFetched> FetchQuestionsRequest { get; private set; }
    
    public Schedule<GameState,CloseCurrentQuestion> CloseCurrentQuestionScheduler { get; set; }
    public Event<CloseCurrentQuestion> CloseCurrentQuestion { get; set; }
    public Event<AnswerQuestion> AnswerQuestion { get; set; }

    public Schedule<GameState,NextQuestion> NextQuestionScheduler { get; set; }
    public Event<NextQuestion> NextQuestion { get; set; }
    
    public Event<GetParticipantState> GetParticipantState { get; set; }
    
   


}

public class GameStateDefinition : SagaDefinition<GameState>
{
    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<GameState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 1000));
        //endpointConfigurator.UseInMemoryOutbox();
    }
}



