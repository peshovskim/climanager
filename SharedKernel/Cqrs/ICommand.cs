using MediatR;

namespace SharedKernel.Cqrs;

public interface ICommand<TResponse> : IRequest<TResponse>;
