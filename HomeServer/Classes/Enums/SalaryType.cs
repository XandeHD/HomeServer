namespace HomeServer.Classes.Enums
{
    public enum SalaryType
    {
        Trabalho,
        Refeicao, // Evitamos caracteres especiais no identificador do código
        Extra,
        RendasCasas
    }

    public static class SalaryTypeExtensions
    {
        public static string ToFriendlyString(this SalaryType type)
        {
            return type switch
            {
                SalaryType.Trabalho => "Trabalho",
                SalaryType.Refeicao => "Refeição",
                SalaryType.Extra => "Extra",
                SalaryType.RendasCasas => "Rendas de Casas",
                _ => type.ToString()
            };
        }
    }
}