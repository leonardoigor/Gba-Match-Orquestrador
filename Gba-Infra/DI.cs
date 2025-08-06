using Gba_Domain.Services;
using Gba_Infra.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gba_Infra
{
    public static class DI
    {
        public static IServiceCollection InjectionDI(this IServiceCollection services) {

            services.AddScoped<IGbaMatchKubernetesService, GbaMatchKubernetesService>();

            return services;
        }
    }
}
