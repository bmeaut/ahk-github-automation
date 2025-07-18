using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ahk.GradeManagement.Backend.Common.Options;

public static class OptionsExtensions
{
    /// <summary>
    /// Megadott típusú options osztályt beregisztrál későbbi DI használatra, és visszaadja az options értékét a Startup.ConfigureServices metódusban való használathoz
    /// </summary>
    /// <typeparam name="TOption">options osztály típusa</typeparam>
    /// <param name="services">DI szolgáltatás konfigurációk</param>
    /// <param name="configuration">App konfiguráció</param>
    /// <param name="sectionName">Konfiguráció szekció neve, ha null kerül átadásra, akkor <typeparamref name="TOption"/> neve lesz használva</param>
    /// <returns><typeparamref name="TOption"/> példány, feltöltve a konfigurációnak megfelelően</returns>
    public static TOption ConfigureOption<TOption>(this IServiceCollection services, IConfiguration configuration, string? sectionName = null)
        where TOption : class, new()
    {
        var options = new TOption();

        // aktuális metódusban való használathoz
        var section = configuration.GetSection(sectionName ?? typeof(TOption).Name);
        section.Bind(options);

        // IOption<TOption> alapú DI használathoz
        services.AddOptions<TOption>(typeof(TOption).Name);
        services.Configure<TOption>(section);

        return options;
    }
}
