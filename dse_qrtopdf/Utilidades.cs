﻿using System;
using System.IO;

namespace dse_qrtopdf
{
    public class Utilidades
    {
        string pathFicheros = System.AppDomain.CurrentDomain.BaseDirectory;
        public string PDFEntrada = string.Empty; //Fichero de entrada
        public string PDFSalida = string.Empty; //Fichero de salida
        public string textoQR = string.Empty; //Texto a insertar en el QR
        public int margen = 4;
        public int anchoQR = 30 + 8 ; //Tamaño del QR en milimetros (se añaden 4mm a derecha e izquierda del margen)
        public float coordenadaX = 5; //Coordenada X desde la esquina superior izquierda
        public float coordenadaY = 5; //Coordenada Y desde la esquina superior izquierda
        public string alineacion = "IZQUIERDA";
        public int dpi = 72; //Resolucion en DPI
        public float pulgadas = 25.4f;
        public int tamañoQRPx = 85; //Tamaño por defecto de 30mm (30 * 72 / 25.4)
        public int margenPx = 11; //Tamaño por defecto de 4mm (4 * 72 / 25.4)




        public void CargaGuion(string guion)
        {
            if (File.Exists(guion))
            {
                string[] lineas = File.ReadAllLines(guion);
                foreach (var linea in lineas)
                {
                    if (string.IsNullOrEmpty(linea)) continue; //Evita lineas vacias

                    string clave = string.Empty;
                    string valor = string.Empty;                    
                    (clave, valor) = divideCadena(linea, '=');
                    
                    if (valor.StartsWith("\"") && valor.EndsWith("\""))
                    {
                        valor = valor.Substring(1, valor.Length - 2);
                    }

                    switch (clave)
                    {
                        case "ENTRADA":
                            //Operador ternario que asigna la ruta de ejecucion si no se pasa la ruta en el guion, o la ruta tal y como viene en el guion
                            PDFEntrada = valor.Contains("\\") ? valor : Path.Combine(pathFicheros, valor);
                            break;

                        case "SALIDA":
                            PDFSalida = valor.Contains("\\") ? valor : Path.Combine(pathFicheros, valor); ;
                            break;

                        case "TEXTOQR":
                            textoQR = valor;
                            break;

                        //case "COORDENADAX":
                        //    //No hara falta si se pone la alineacion derecha o izquierda
                        //    coordenadaX = float.Parse(valor);
                        //    break;

                        //case "COORDENADAY":
                        //    //No hara falta si se pone la alineacion derecha o izquierda
                        //    coordenadaY = float.Parse(valor);
                        //    break;

                        case "TAMAÑO":
                            // Convertir tamaño de mm a píxeles (suponiendo 96 DPI)
                            float contenido = float.Parse(valor);
                            tamañoQRPx = (int)convierteAPx(contenido);
                            break;

                        case "ALINEACION":
                            alineacion = valor;
                            if (alineacion == "DERECHA") coordenadaX = convierteAPx(210 - anchoQR -  (margen * 2));
                            break;

                        case "MARGEN":
                            margenPx = (int)convierteAPx(Convert.ToInt32(valor));
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
            if (partes.Length == 2)
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

    }
}
