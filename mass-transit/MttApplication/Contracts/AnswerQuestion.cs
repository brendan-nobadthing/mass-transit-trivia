using MassTransit;
using MttApplication.Entities;

namespace MttApplication.Contracts;


public class AnswerQuestion: QuestionResponse, CorrelatedBy<Guid>
{
}