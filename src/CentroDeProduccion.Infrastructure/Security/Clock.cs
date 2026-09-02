using CentroDeProduccion.Application.Abstractions.Time;

namespace CentroDeProduccion.Infrastructure.Security;

public class Clock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
