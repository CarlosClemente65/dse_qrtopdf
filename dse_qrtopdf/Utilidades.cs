using System;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text.pdf;

namespace dse_qrtopdf
{
    public class Utilidades
    {
        string pathFicheros = System.AppDomain.CurrentDomain.BaseDirectory;
        public string PDFEntrada = string.Empty; //Fichero de entrada
        public string PDFSalida = string.Empty; //Fichero de salida
        public string textoQR = string.Empty; //Texto a insertar en el QR
        public bool VERIFACTU = false; //Si se quiere añadir los textos oficiales de VERI-FACTU
        public string marcaAgua = string.Empty; //Texto a insertar como marca de agua
        public int margen = 2; //Representa la distancia respecto al margen izquierdo / derecho a añadir al recuadro del QR
        public int anchoQR = 30; //Tamaño del QR en milimetros
        public string alineacion = "IZQUIERDA";
        public int dpi = 96; //Resolucion en DPI
        public float pulgadas = 25.4f;
        public int tamañoQRPx = 113; //Tamaño por defecto de 30mm (30 * 96 / 25.4)
        public int margenPx = 8; //Tamaño por defecto de 2mm (2 * 96 / 25.4)
        public string proceso = string.Empty;




        public void CargaGuion(string guion)
        {
            if(File.Exists(guion))
            {
                string[] lineas = File.ReadAllLines(guion);
                foreach(var linea in lineas)
                {
                    if(string.IsNullOrEmpty(linea)) continue; //Evita lineas vacias

                    string clave = string.Empty;
                    string valor = string.Empty;
                    (clave, valor) = divideCadena(linea, '=');

                    if(valor.StartsWith("\"") && valor.EndsWith("\""))
                    {
                        valor = valor.Substring(1, valor.Length - 2);
                    }

                    switch(clave)
                    {
                        case "ENTRADA":
                            //Operador ternario que asigna la ruta de ejecucion si no se pasa la ruta en el guion, o la ruta tal y como viene en el guion
                            PDFEntrada = valor.Contains("\\") ? valor : Path.Combine(pathFicheros, valor);
                            break;

                        case "SALIDA":
                            PDFSalida = valor.Contains("\\") ? valor : Path.Combine(pathFicheros, valor);
                            break;

                        case "TEXTOQR":
                            textoQR = valor;
                            break;

                        case "TAMAÑO":
                            // Convertir tamaño de mm a píxeles (suponiendo 96 DPI)
                            anchoQR = Convert.ToInt32(valor);
                            tamañoQRPx = (int)convierteAPx(anchoQR);
                            break;

                        case "ALINEACION":
                            alineacion = valor.ToUpper();
                            break;

                        case "MARGEN":
                            margen = Convert.ToInt32(valor);
                            margenPx = (int)convierteAPx(margen);
                            break;

                        case "TIPO":
                            proceso = valor;
                            break;

                        case "MARCA_AGUA":
                            string texto = valor.Replace("\\n", "\n");
                            marcaAgua = valor;
                            break;

                        case "VERIFACTU":
                            if(valor.ToUpper() == "SI")
                            {
                                VERIFACTU = true;
                            }
                            break;

                    }
                }
            }
        }

        public (string, string) divideCadena(string cadena, char divisor)
        {
            //Permite dividir una cadena por el divisor pasado y solo la divide en un maximo de 2 partes (divide desde el primer divisor que encuentra)
            string atributo = string.Empty;
            string valor = string.Empty;
            string[] partes = cadena.Split(new[] { divisor }, 2);
            if(partes.Length == 2)
            {
                atributo = partes[0].Trim();
                valor = partes[1].Trim();
            }

            return (atributo, valor);
        }

        public float convierteAPx(float valor)
        {
            return valor * (float)dpi / pulgadas;
        }


        public List<string> DividirTextoEnLineas(string texto, BaseFont fuente, float tamFuente, float anchoMaximo)
        {
            List<string> lineas = new List<string>();

            // Dividir el texto segun los saltos de linea reales
            string[] parrafos = texto.Split(new[] { "\r\n", "\\n" }, StringSplitOptions.None);

            foreach(string parrafo in parrafos)
            {
                if(string.IsNullOrEmpty(parrafo))
                {
                    lineas.Add(string.Empty); // Añadir una línea vacía para los saltos de línea
                    continue; // Evitar lineas vacias
                }

                float anchoParrafo = fuente.GetWidthPoint(parrafo, tamFuente); // Obtener el ancho del parrafo
                if(anchoParrafo <= anchoMaximo)
                {
                    lineas.Add(parrafo); // Si el ancho del parrafo es menor que el ancho maximo, añadirlo directamente
                }
                else
                {

                    string[] palabras = parrafo.Split(' ');
                    string lineaActual = "";

                    foreach(string palabra in palabras)
                    {
                        string prueba = string.IsNullOrEmpty(lineaActual) ? palabra : lineaActual + " " + palabra;
                        float ancho = fuente.GetWidthPoint(prueba, tamFuente);

                        if(ancho <= anchoMaximo)
                        {
                            lineaActual = prueba;
                        }
                        else
                        {
                            if(!string.IsNullOrEmpty(lineaActual))
                            {
                                lineas.Add(lineaActual);
                            }
                            lineaActual = palabra;
                        }
                    }

                    if(!string.IsNullOrEmpty(lineaActual))
                    {
                        lineas.Add(lineaActual);
                    }
                }
            }

            return lineas;
        }
    }
}
