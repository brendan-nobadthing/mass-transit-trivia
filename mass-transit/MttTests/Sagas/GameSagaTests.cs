using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using MttApplication.Contracts;
using MttApplication.Entities;
using MttApplication.Sagas;
using Shouldly;

namespace MttTests.Sagas;

public class GameSagaTests
{
    
    [Fact]
    public async Task CreateGameShouldLobbyOpen()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();

        // act
        await CreateGame(harness, sagaId);

        // assert
        var instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, 
                sagaHarness.StateMachine.LobbyOpen);

        instance.ShouldNotBeNull();
        instance.CurrentState.ShouldBe("LobbyOpen");
    }
    
    
    [Fact]
    public async Task GivenLobbyOpen_WhenAddParticipant_Success()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();
        await CreateGame(harness, sagaId);
        
        // act
        await AddParticipant(harness, sagaId, Participants[0]);
        await AddParticipant(harness, sagaId, Participants[1]);

        // assert
        var instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.LobbyOpen);
        instance.ShouldNotBeNull();
        instance.CurrentState.ShouldBe("LobbyOpen");
        instance.Participants.Count.ShouldBe(2);
    }
    
    
    [Fact]
    public async Task GivenLobbyOpen_WhenStartGame_FetchQuestionsPending()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();
        harness.AddSagaInstance<GameState>(sagaId, s =>
        {
            s.CurrentState = "LobbyOpen";
            s.Participants = Participants;
        });
        
        // act
        await harness.Bus.Publish<StartGame>(new StartGame(){CorrelationId = sagaId});
        await harness.Consumed.Any<StartGame>();
        

        // assert
        var instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.FetchQuestionsRequest.Pending);
        instance.ShouldNotBeNull();
        instance.Participants.Count.ShouldBe(2);
    }

    
    [Fact]
    public async Task GivenFetchQuestions_WhenQuestionsFetched_QuestionOpen_AndCloseQuestionScheduled()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();
        harness.AddSagaInstance<GameState>(sagaId, s =>
        {
            s.CurrentState = "LobbyOpen";
            s.Participants = Participants;
            s.AnswerTimeSeconds = 1;
        });
        
        // act
        await harness.Bus.Publish<StartGame>(new StartGame(){CorrelationId = sagaId});
        (await sagaHarness.Consumed.Any<StartGame>()).ShouldBeTrue();
        (await sagaHarness.Consumed.Any<QuestionsFetched>()).ShouldBeTrue();
        
        // assert
        var instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.QuestionOpen);
        instance.ShouldNotBeNull();
        instance.CurrentState.ShouldNotBeNull();
        instance.CurrentQuestionIndex.ShouldBe(0);
        instance.CloseCurrentQuestionSchedulerTokenId.ShouldNotBeNull();

        (await sagaHarness.Consumed.Any<CloseCurrentQuestion>()).ShouldBeTrue();
        instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.QuestionResult);
        instance.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task GivenLobbyOpen_WhenGetParticipantState_NoCurrentQuestion()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();
        harness.AddSagaInstance<GameState>(sagaId, s =>
        {
            s.CurrentState = "LobbyOpen";
            s.Participants = Participants;
        });
        
        // act
        var client = harness.GetRequestClient<GetParticipantState>();
        var response = await client.GetResponse<ParticipantStateResponse>(new GetParticipantState()
        {
            CorrelationId = sagaId,
            ParticipantId = Participants[0].ParticipantId
        });
        
        // assert
        response.Message.CorrelationId.ShouldBe(sagaId);
        response.Message.CurrentState.ShouldBe("LobbyOpen");
        response.Message.CurrentQuestionIndex.ShouldBe(null);
        response.Message.CurrentQuestion.ShouldBe(null);
    }


    [Fact]
    public async Task GivenQuestionOpen_WhenQuestionAnswered_Success()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();
        harness.AddSagaInstance<GameState>(sagaId, s =>
        {
            s.CurrentState = "QuestionOpen";
            s.Participants = Participants;
            s.Questions = MockFetchQuestionsQuestionsConsumer.Questions;
            s.CurrentQuestionIndex = 0;
        });
        
        // act
        await harness.Bus.Publish<AnswerQuestion>(new AnswerQuestion()
        {
            CorrelationId = sagaId,
            ParticipantId = Participants[0].ParticipantId,
            QuestionIndex = 0,
            Answer = "Q1 Correct"
        });
        await harness.Consumed.Any<AnswerQuestion>();
        
        // assert
        var instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.QuestionOpen);
        instance.ShouldNotBeNull();
        instance.Responses.Count.ShouldBe(1);
    } 
    
    
    [Fact]
    public async Task GivenQuestionOpen_WhenCloseCurrentQuestion_QuestionResult_AndNextQuestionScheduled()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();
        harness.AddSagaInstance<GameState>(sagaId, s =>
        {
            s.CurrentState = "QuestionOpen";
            s.Participants = Participants;
            s.Questions = MockFetchQuestionsQuestionsConsumer.Questions;
            s.CurrentQuestionIndex = 0;
            s.ShowResultTimeSeconds = 1;
        });
        
        // act
        await harness.Bus.Publish<CloseCurrentQuestion>(new CloseCurrentQuestion()
        {
            CorrelationId = sagaId
        });
        (await sagaHarness.Consumed.Any<CloseCurrentQuestion>()).ShouldBeTrue();
        
        // assert QuestionResponse 
        var instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.QuestionResult);
        instance.ShouldNotBeNull();

        // assert NextQuestion Scheduled
        (await sagaHarness.Consumed.Any<NextQuestion>()).ShouldBeTrue();
        instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.QuestionOpen);
        instance.CurrentQuestionIndex.ShouldBe(1);
    } 
    
    
    
    [Fact]
    public async Task GivenQuestionOpen_WhenCloseCurrentQuestion_ScoresApplied()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();
        harness.AddSagaInstance<GameState>(sagaId, s =>
        {
            s.CurrentState = "QuestionOpen";
            s.Participants = Participants;
            s.Questions = MockFetchQuestionsQuestionsConsumer.Questions;
            s.CurrentQuestionIndex = 0;
            s.ShowResultTimeSeconds = 1;
            s.Responses = CorrectResponses;
        });
        
        // act
        await harness.Bus.Publish<CloseCurrentQuestion>(new CloseCurrentQuestion()
        {
            CorrelationId = sagaId
        });
        (await sagaHarness.Consumed.Any<CloseCurrentQuestion>()).ShouldBeTrue();
        
        // assert QuestionResponse 
        var instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.QuestionResult);
        instance.ShouldNotBeNull();

        // assert scores calculated
        instance.Scores.ShouldBeEmpty();
        instance.Scores.FirstOrDefault(s => s.ParticipantId == Participants[0].ParticipantId)?.Score.ShouldBe(4);
        instance.Scores.FirstOrDefault(s => s.ParticipantId == Participants[1].ParticipantId)?.Score.ShouldBe(1);
        
    } 
    
    [Fact]
    public async Task GivenQuestionOpenWithIncorrrectAnswers_WhenCloseCurrentQuestion_ScoresApplied()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();
        harness.AddSagaInstance<GameState>(sagaId, s =>
        {
            s.CurrentState = "QuestionOpen";
            s.Participants = Participants;
            s.Questions = MockFetchQuestionsQuestionsConsumer.Questions;
            s.CurrentQuestionIndex = 0;
            s.ShowResultTimeSeconds = 1;
            s.Responses = IncorrectResponses;
        });
        
        // act
        await harness.Bus.Publish<CloseCurrentQuestion>(new CloseCurrentQuestion()
        {
            CorrelationId = sagaId
        });
        (await sagaHarness.Consumed.Any<CloseCurrentQuestion>()).ShouldBeTrue();
        
        // assert QuestionResponse 
        var instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.QuestionResult);
        instance.ShouldNotBeNull();

        // assert scores calculated
        instance.Scores.ShouldBeEmpty();
        instance.Scores.FirstOrDefault(s => s.ParticipantId == Participants[0].ParticipantId)?.Score.ShouldBe(-2);
        instance.Scores.FirstOrDefault(s => s.ParticipantId == Participants[1].ParticipantId)?.Score.ShouldBe(-1);
        
    } 
    
    
    
    [Fact]
    public async Task GivenLastQuestionOpen_WhenCloseCurrentQuestion_QuestionResult_AndNextQuestionScheduled_AndFinal()
    {
        // arrange
        await using var provider = BuildProvider();
        var harness = await StartTestHarness(provider);
        var sagaId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<GameStateMachine, GameState>();
        harness.AddSagaInstance<GameState>(sagaId, s =>
        {
            s.CurrentState = "QuestionOpen";
            s.Participants = Participants;
            s.Questions = MockFetchQuestionsQuestionsConsumer.Questions;
            s.CurrentQuestionIndex = s.Questions.Count-1;
            s.ShowResultTimeSeconds = 1;
        });
        
        // act
        await harness.Bus.Publish<CloseCurrentQuestion>(new CloseCurrentQuestion()
        {
            CorrelationId = sagaId
        });
        (await sagaHarness.Consumed.Any<CloseCurrentQuestion>()).ShouldBeTrue();
        
        // assert QuestionResponse 
        var instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.QuestionResult);
        instance.ShouldNotBeNull();

        // assert NextQuestion Scheduled -> Final
        (await sagaHarness.Consumed.Any<NextQuestion>()).ShouldBeTrue();
        instance =
            sagaHarness.Sagas.ContainsInState(sagaId, sagaHarness.StateMachine, sagaHarness.StateMachine.Final);
        instance.CurrentQuestionIndex.ShouldBe(1);
    } 
    
    
    private static async Task CreateGame(ITestHarness harness, Guid sagaId)
    {
        await harness.Bus.Publish(new CreateGame()
        {
            CorrelationId = sagaId
        });
        (await harness.Consumed.Any<CreateGame>()).ShouldBeTrue();
    }

    private async Task AddParticipant(ITestHarness harness, Guid sagaId, Participant participant)
    {
        await harness.Bus.Publish(new AddParticipant()
        {
            CorrelationId = sagaId,
            Participant = participant
        });
        await harness.Consumed.Any<AddParticipant>(p =>
            p.Context.Message.Participant.ParticipantId == participant.ParticipantId);
    }

    private IList<Participant> Participants => new List<Participant>()
    {
        new Participant()
        {
            ParticipantId = Guid.NewGuid(),
            DisplayName = "Participant One"
        },
        new Participant()
        {
            ParticipantId = Guid.NewGuid(),
            DisplayName = "Participant Two"
        }
    };
    
    
    private IList<QuestionResponse> CorrectResponses => new List<QuestionResponse>()
    {
        new QuestionResponse()
        {
            ParticipantId = Participants[0].ParticipantId,
            Answer = MockFetchQuestionsQuestionsConsumer.Questions[0].CorrectAnswer,
            Timestamp = new DateTime(2023,1,1).AddSeconds(0.8) // should be 4 points
        },
        new QuestionResponse()
        {
            ParticipantId = Participants[1].ParticipantId,
            Answer = MockFetchQuestionsQuestionsConsumer.Questions[0].CorrectAnswer,
            Timestamp = new DateTime(2023,1,1).AddSeconds(3.3) // should be 1 point
        }
    };
    
    private IList<QuestionResponse> IncorrectResponses => new List<QuestionResponse>()
    {
        new QuestionResponse()
        {
            ParticipantId = Participants[0].ParticipantId,
            Answer = MockFetchQuestionsQuestionsConsumer.Questions[0].IncorrectAnswers[0],
            Timestamp = new DateTime(2023,1,1).AddSeconds(0.8) // should be -2 points
        },
        new QuestionResponse()
        {
            ParticipantId = Participants[1].ParticipantId,
            Answer = MockFetchQuestionsQuestionsConsumer.Questions[0].IncorrectAnswers[0],
            Timestamp = new DateTime(2023,1,1).AddSeconds(3.3) // should be -1 point
        }
    };
    


    private ServiceProvider BuildProvider()
    {
        return new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<GameStateMachine, GameState>();
                cfg.AddConsumer<MockFetchQuestionsQuestionsConsumer>();
            })
            .BuildServiceProvider(true);
    }

    private async Task<ITestHarness> StartTestHarness(IServiceProvider provider)
    {
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        return harness;
    }

    

    private class MockFetchQuestionsQuestionsConsumer: IConsumer<FetchQuestions>
    {
        public async Task Consume(ConsumeContext<FetchQuestions> context)
        {
            await context.RespondAsync(new QuestionsFetched()
            {
                CorrelationId = context.Message.CorrelationId,
                Questions = Questions
            });
        }
        
        public static List<Question> Questions => new List<Question>()
        {
            new Question()
            {
                QuestionIndex = 0,
                QuestionText = "Question 1 Text?", 
                CorrectAnswer = "Q1 Correct",
                IncorrectAnswers = new List<string>() { "Q1 Wrong1", "Q1 Wrong2", "Q1 Wrong3" },
                QuestionOpened = new DateTime(2023,1,1),
                QuestionClosed = new DateTime(2023,1,1).AddSeconds(4)
            },
            new Question()
            {
                QuestionIndex = 1,
                QuestionText = "Question 2 Text?", 
                CorrectAnswer = "Q2 Correct",
                IncorrectAnswers = new List<string>() { "Q2 Wrong1", "Q2 Wrong2", "Q2 Wrong3" },
                QuestionOpened = new DateTime(2023,1,1),
                QuestionClosed = new DateTime(2023,1,1).AddSeconds(4)
            },
        };
    }
}