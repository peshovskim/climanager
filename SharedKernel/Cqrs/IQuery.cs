using MediatR;

namespace SharedKernel.Cqrs;

public interface IQuery<TResponse> : IRequest<TResponse>;
