using System;
using System.Collections.Generic;
using Boleto2Net.Exceptions;
using Boleto2Net.Extensions;
using Microsoft.VisualBasic;

namespace Boleto2Net
{
    public sealed class Banco : IBanco
    {
        private static readonly Dictionary<int, Lazy<IBanco>> Bancos = new Dictionary<int, Lazy<IBanco>>
        {
            [001] = BancoBrasil.Instance,
            [033] = BancoSantander.Instance,
            [104] = BancoCaixa.Instance,
            [237] = BancoBradesco.Instance,
            [341] = BancoItau.Instance,
            [756] = BancoSicoob.Instance
        };

        private readonly IBanco _banco;

        public Banco(int codigoBanco)
            => _banco = (Bancos.ContainsKey(codigoBanco) ? Bancos[codigoBanco] : throw Boleto2NetException.BancoNaoImplementado(codigoBanco)).Value;

        public int Codigo => _banco.Codigo;

        public string Digito => _banco.Digito;

        public string Nome => _banco.Nome;

        public bool RemoveAcentosArquivoRemessa => _banco.RemoveAcentosArquivoRemessa;

        public Cedente Cedente
        {
            get => _banco.Cedente;
            set => _banco.Cedente = value;
        }

        public List<string> IdsRetornoCnab400RegistroDetalhe => _banco.IdsRetornoCnab400RegistroDetalhe;

        public Boleto NovoBoleto()
        {
            var boleto = new Boleto(_banco);
            return boleto;
        }

        /// <summary>
        ///     Formata Cedente
        /// </summary>
        public void FormataCedente()
        {
            try
            {
                _banco.FormataCedente();
            }
            catch (Exception ex)
            {
                throw Boleto2NetException.ErroAoFormatarCedente(ex);
            }
        }

        /// <summary>
        ///     Formata Campo Livre
        /// </summary>
        public string FormataCodigoBarraCampoLivre(Boleto boleto)
        {
            try
            {
                var campoLivre = _banco.FormataCodigoBarraCampoLivre(boleto);
                if (campoLivre.Length != 25)
                    throw new Exception($"Campo Livre deve ter 25 posi��es: {campoLivre}");
                return campoLivre;
            }
            catch (Exception ex)
            {
                throw Boleto2NetException.ErroAoFormatarCodigoDeBarra(ex);
            }
        }

        /// <summary>
        ///     Formata nosso n�mero
        /// </summary>
        public void FormataNossoNumero(Boleto boleto)
        {
            try
            {
                _banco.FormataNossoNumero(boleto);
            }
            catch (Exception ex)
            {
                throw Boleto2NetException.ErroAoFormatarNossoNumero(ex);
            }
        }

        /// <summary>
        ///     Valida o boleto
        /// </summary>
        public void ValidaBoleto(Boleto boleto)
        {
            try
            {
                // Valida os dados do Boleto na classe do banco respons�vel
                _banco.ValidaBoleto(boleto);
                // Formata nosso n�mero (Classe Abstrata)
                FormataNossoNumero(boleto);
                // Formata cedente (Classe Abstrata)
                FormataCedente();
                // Formata o c�digo de Barras (Classe Abstrata)
                FormataCodigoBarra(boleto);
                // Formata linha digitavel (Classe Abstrata)
                FormataLinhaDigitavel(boleto);
            }
            catch (Exception ex)
            {
                throw Boleto2NetException.ErroAoValidarBoleto(ex);
            }
        }

        /// <summary>
        ///     Formata c�digo de barras
        ///     O c�digo de barra para cobran�a cont�m 44 posi��es dispostas da seguinte forma:
        ///     01 a 03 - 3 - Identifica��o  do  Banco
        ///     04 a 04 - 1 - C�digo da Moeda
        ///     05 a 05 � 1 - D�gito verificador do C�digo de Barras
        ///     06 a 09 - 4 - Fator de vencimento
        ///     10 a 19 - 10 - Valor
        ///     20 a 44 � 25 - Campo Livre
        /// </summary>
        public void FormataCodigoBarra(Boleto boleto)
        {
            var codigoBarra = boleto.CodigoBarra;
            codigoBarra.CampoLivre = FormataCodigoBarraCampoLivre(boleto);
            if (string.IsNullOrWhiteSpace(codigoBarra.CampoLivre))
            {
                codigoBarra.CodigoBanco = string.Empty;
                codigoBarra.Moeda = 0;
                codigoBarra.FatorVencimento = 0;
                codigoBarra.ValorDocumento = string.Empty;
            }
            else
            {
                codigoBarra.CodigoBanco = Utils.FitStringLength(Codigo.ToString(), 3, 3, '0', 0, true, true, true);
                codigoBarra.Moeda = boleto.CodigoMoeda;
                codigoBarra.FatorVencimento = boleto.DataVencimento.FatorVencimento();
                codigoBarra.ValorDocumento = boleto.ValorTitulo.ToString("N2").Replace(",", "").Replace(".", "").PadLeft(10, '0');
            }
        }

        /// <summary>
        ///     A linha digit�vel ser� composta por cinco campos:
        ///     1� campo
        ///     composto pelo c�digo de Banco, c�digo da moeda, as cinco primeiras posi��es do campo
        ///     livre e o d�gito verificador deste campo;
        ///     2� campo
        ///     composto pelas posi��es 6� a 15� do campo livre e o d�gito verificador deste campo;
        ///     3� campo
        ///     composto pelas posi��es 16� a 25� do campo livre e o d�gito verificador deste campo;
        ///     4� campo
        ///     composto pelo d�gito verificador do c�digo de barras, ou seja, a 5� posi��o do c�digo de
        ///     barras;
        ///     5� campo
        ///     Composto pelo fator de vencimento com 4(quatro) caracteres e o valor do documento com 10(dez) caracteres, sem
        ///     separadores e sem edi��o.
        /// </summary>
        public void FormataLinhaDigitavel(Boleto boleto)
        {
            var codigoBarra = boleto.CodigoBarra;
            if (string.IsNullOrWhiteSpace(codigoBarra.CampoLivre))
            {
                codigoBarra.LinhaDigitavel = "";
                return;
            }
            //BBBMC.CCCCD1 CCCCC.CCCCCD2 CCCCC.CCCCCD3 D4 FFFFVVVVVVVVVV

            var codigoDeBarras = codigoBarra.CodigoDeBarras;
            #region Campo 1

            // POSI��O 1 A 3 DO CODIGO DE BARRAS
            var bbb = codigoDeBarras.Substring(0, 3);
            // POSI��O 4 DO CODIGO DE BARRAS
            var m = codigoDeBarras.Substring(3, 1);
            // POSI��O 20 A 24 DO CODIGO DE BARRAS
            var ccccc = codigoDeBarras.Substring(19, 5);
            // Calculo do D�gito
            var d1 = CalcularDvModulo10(bbb + m + ccccc);
            // Formata Grupo 1
            var grupo1 = $"{bbb}{m}{ccccc.Substring(0, 1)}.{ccccc.Substring(1, 4)}{d1} ";

            #endregion Campo 1

            #region Campo 2

            //POSI��O 25 A 34 DO COD DE BARRAS
            var d2A = codigoDeBarras.Substring(24, 10);
            // Calculo do D�gito
            var d2B = CalcularDvModulo10(d2A).ToString();
            // Formata Grupo 2
            var grupo2 = $"{d2A.Substring(0, 5)}.{d2A.Substring(5, 5)}{d2B} ";

            #endregion Campo 2

            #region Campo 3

            //POSI��O 35 A 44 DO CODIGO DE BARRAS
            var d3A = codigoDeBarras.Substring(34, 10);
            // Calculo do D�gito
            var d3B = CalcularDvModulo10(d3A).ToString();
            // Formata Grupo 3
            var grupo3 = $"{d3A.Substring(0, 5)}.{d3A.Substring(5, 5)}{d3B} ";

            #endregion Campo 3

            #region Campo 4

            // D�gito Verificador do C�digo de Barras
            var grupo4 = $"{codigoBarra.DigitoVerificador} ";

            #endregion Campo 4

            #region Campo 5

            //POSICAO 6 A 9 DO CODIGO DE BARRAS
            var d5A = codigoDeBarras.Substring(5, 4);
            //POSICAO 10 A 19 DO CODIGO DE BARRAS
            var d5B = codigoDeBarras.Substring(9, 10);
            // Formata Grupo 5
            var grupo5 = $"{d5A}{d5B}";

            #endregion Campo 5

            codigoBarra.LinhaDigitavel = $"{grupo1}{grupo2}{grupo3}{grupo4}{grupo5}";
        }

        private static int CalcularDvModulo10(string texto)
        {
            int soma = 0, peso = 2;
            for (var i = texto.Length; i > 0; i--)
            {
                var resto = Convert.ToInt32(Strings.Mid(texto, i, 1)) * peso;
                if (resto > 9)
                    resto = resto / 10 + resto % 10;
                soma += resto;
                if (peso == 2)
                    peso = 1;
                else
                    peso = peso + 1;
            }
            var digito = (10 - soma % 10) % 10;
            return digito;
        }

        #region Gerar Arquivo Remessa

        /// <summary>
        ///     Gera os registros de header do aquivo de remessa
        /// </summary>
        public string GerarHeaderRemessa(TipoArquivo tipoArquivo, int numeroArquivoRemessa, ref int numeroRegistroGeral)
        {
            try
            {
                return _banco.GerarHeaderRemessa(tipoArquivo, numeroArquivoRemessa, ref numeroRegistroGeral);
            }
            catch (Exception ex)
            {
                throw Boleto2NetException.ErroAoGerarRegistroHeaderDoArquivoRemessa(ex);
            }
        }

        /// <summary>
        ///     Gera registros de detalhe do arquivo remessa
        /// </summary>
        public string GerarDetalheRemessa(TipoArquivo tipoArquivo, Boleto boleto, ref int numeroRegistro)
        {
            try
            {
                return _banco.GerarDetalheRemessa(tipoArquivo, boleto, ref numeroRegistro);
            }
            catch (Exception ex)
            {
                throw Boleto2NetException.ErroAoGerarRegistroDetalheDoArquivoRemessa(ex);
            }
        }

        /// <summary>
        ///     Gera os registros de Trailer do arquivo de remessa
        /// </summary>
        public string GerarTrailerRemessa(TipoArquivo tipoArquivo, int numeroArquivoRemessa,
            ref int numeroRegistroGeral, decimal valorBoletoGeral,
            int numeroRegistroCobrancaSimples, decimal valorCobrancaSimples,
            int numeroRegistroCobrancaVinculada, decimal valorCobrancaVinculada,
            int numeroRegistroCobrancaCaucionada, decimal valorCobrancaCaucionada,
            int numeroRegistroCobrancaDescontada, decimal valorCobrancaDescontada)
        {
            try
            {
                return _banco.GerarTrailerRemessa(tipoArquivo, numeroArquivoRemessa,
                    ref numeroRegistroGeral, valorBoletoGeral,
                    numeroRegistroCobrancaSimples, valorCobrancaSimples,
                    numeroRegistroCobrancaVinculada, valorCobrancaVinculada,
                    numeroRegistroCobrancaCaucionada, valorCobrancaCaucionada,
                    numeroRegistroCobrancaDescontada, valorCobrancaDescontada);
            }
            catch (Exception ex)
            {
                throw Boleto2NetException.ErroAoGerrarRegistroTrailerDoArquivoRemessa(ex);
            }
        }

        #endregion

        #region Leitura do Arquivo Retorno

        public void LerDetalheRetornoCNAB240SegmentoT(ref Boleto boleto, string registro)
            => _banco.LerDetalheRetornoCNAB240SegmentoT(ref boleto, registro);

        public void LerDetalheRetornoCNAB240SegmentoU(ref Boleto boleto, string registro)
            => _banco.LerDetalheRetornoCNAB240SegmentoU(ref boleto, registro);

        public void LerHeaderRetornoCNAB400(string registro)
            => _banco.LerHeaderRetornoCNAB400(registro);

        public void LerDetalheRetornoCNAB400Segmento1(ref Boleto boleto, string registro)
            => _banco.LerDetalheRetornoCNAB400Segmento1(ref boleto, registro);

        public void LerDetalheRetornoCNAB400Segmento7(ref Boleto boleto, string registro)
            => _banco.LerDetalheRetornoCNAB400Segmento7(ref boleto, registro);

        public void LerTrailerRetornoCNAB400(string registro)
            => _banco.LerTrailerRetornoCNAB400(registro);

        #endregion
    }
}