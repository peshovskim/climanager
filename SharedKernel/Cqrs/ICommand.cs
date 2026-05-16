using MediatR;

namespace SharedKernel.Cqrs;

public interface ICommand : IRequest;

public interface ICommand<TResponse> : IRequest<TResponse>;
