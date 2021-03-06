namespace Boleto2Net
{
    /// <summary>
    /// Representa o endere�o do Cedente ou Sacado.
    /// </summary>
    public class Endereco
    {
        public string LogradouroEndereco { get; set; } = string.Empty;
        public string LogradouroNumero { get; set; } = string.Empty;
        public string LogradouroComplemento { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string UF { get; set; } = string.Empty;
        public string CEP { get; set; } = string.Empty;

        public string FormataLogradouro(int tamanhoFinal)
        {
            /*var logradouroCompleto = string.Empty;
            if (LogradouroNumero.Length != 0)
                logradouroCompleto += " " + LogradouroNumero;
            if (LogradouroComplemento.Length != 0)
                logradouroCompleto += " " + LogradouroComplemento;

            if (tamanhoFinal == 0)
                return LogradouroEndereco + logradouroCompleto;

            if (LogradouroEndereco.Length + logradouroCompleto.Length <= tamanhoFinal)
                return LogradouroEndereco + logradouroCompleto;

            return LogradouroEndereco.Substring(0, tamanhoFinal - logradouroCompleto.Length);*/
            string endereco = string.Format("{0} {1} {2}", LogradouroEndereco, LogradouroNumero, LogradouroComplemento);

            if(endereco.Length < tamanhoFinal || tamanhoFinal == 0)
            {
                return endereco;
            }
            else
            {
                return endereco.Substring(0, tamanhoFinal);
            }
                        
        }
    }
}
