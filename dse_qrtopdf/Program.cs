using System;
using System.Drawing;
using System.IO;
using iTextSharp.text.pdf;
using Image = iTextSharp.text.Image;
using dse_qrtopdf;
using ZXing;
using iTextSharp.text;
using System.Runtime.Remoting.Messaging;
using System.Drawing.Imaging;
using System.Collections.Generic;

class Program
{

    static Utilidades utiles = new Utilidades();

    static void Main(string[] argumentos)
    {
        //Carga los datos del guion
        if(argumentos.Length > 0)
        {
            utiles.CargaGuion(argumentos[0]);
        }
        else
        {
            File.WriteAllText("errores.txt", "No se ha pasado el guion");
            Environment.Exit(0);
        }

        switch(utiles.proceso)
        {
            case "1":
                //Genera el QR (tamaño en mm)
                var imagenQR = GeneradorQR();

                // Crear el PDF e insertar la imagen QR
                InsertaQRenPDF(imagenQR);

                break;

            case "2":
                AgregarMarcaDeAgua();

                break;
        }
    }


    public static Bitmap GeneradorQR()
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = utiles.tamañoQRPx,
                Height = utiles.tamañoQRPx,
                Margin = 0,
                NoPadding = true //Se quitan los espacios del QR para luego situarlo mejor en el bitmap
            }
        };

        Bitmap qrImage = writer.Write(utiles.textoQR);

        //Seleccion del tipo de fuente
        System.Drawing.Font font = new System.Drawing.Font("Arial", 8, FontStyle.Bold);
        Brush pincel = Brushes.Black;
        int altoFuente = font.Height;

        //Medidas del bitmap
        int anchoBitmap = qrImage.Width;
        int altoBitmap = qrImage.Height + (altoFuente * 3); //Se multiplica por 3 para añadir una separacion entre el texto y el QR ya que se posiciona en el centro del cuadro de imagen

        Bitmap bitmapConTexto = new Bitmap(anchoBitmap, altoBitmap);

        using(Graphics graphics = Graphics.FromImage(bitmapConTexto))
        {
            graphics.Clear(Color.White);

            // Dibujar el código QR primero para luego colocar encima los textos
            graphics.DrawImage(qrImage, 0, (bitmapConTexto.Height - qrImage.Height) / 2, qrImage.Width, qrImage.Height); //Se posiciona en el centro del eje Y (altura)

            if(utiles.VERIFACTU)
            {
                // Dibujar el texto superior
                string textoSup = "QR Tributario";
                SizeF tamañoTextoSup = graphics.MeasureString(textoSup, font);
                float posicionXSup = (anchoBitmap - tamañoTextoSup.Width) / 2; // Centrar el texto horizontalmente
                float posicionYSup = 0; //Se pone pegado al borde superior
                graphics.DrawString(textoSup, font, pincel, posicionXSup, posicionYSup);

                // Dibujar el texto inferior
                string textoInf = "VERI*FACTU";
                SizeF tamañoTextoInf = graphics.MeasureString(textoInf, font);
                float posicionXInf = (anchoBitmap - tamañoTextoInf.Width) / 2; // Centrar el texto
                float posicionYInf = altoBitmap - altoFuente;
                graphics.DrawString(textoInf, font, pincel, posicionXInf, posicionYInf);
            }
        }

        return bitmapConTexto;
    }


    static void InsertaQRenPDF(Bitmap imagenQR)
    {
        using(PdfReader reader = new PdfReader(utiles.PDFEntrada))
        {
            using(FileStream fs = new FileStream(utiles.PDFSalida, FileMode.Create))
            {
                using(PdfStamper stamper = new PdfStamper(reader, fs))
                {
                    // Obtener la primera página del PDF 
                    PdfContentByte content = stamper.GetOverContent(1); // Índice de la página (1 es la primera página)

                    float x = utiles.alineacion != "DERECHA"
                        ? utiles.margen * 72 / 25.4f
                        : 595 - imagenQR.Width - utiles.convierteAPx(utiles.margen); //Se calcula la posicion x en funcion de si esta alineado a la izquierda o derecha

                    float y = reader.GetPageSize(1).Height - imagenQR.Height - utiles.margen * 72 / 25.4f; //Se pasan los milimetros a dpi (mm * 72 / 25.4)

                    // Convertir Bitmap en iTextSharp imagen
                    using(var stream = new MemoryStream())
                    {
                        imagenQR.SetResolution(utiles.dpi, utiles.dpi);
                        imagenQR.Save(stream, ImageFormat.Png);
                        Image textoImagenQR = Image.GetInstance(stream.ToArray());
                        textoImagenQR.ScaleAbsolute(imagenQR.Width, imagenQR.Height); // Ajustar las dimensiones en el PDF
                        textoImagenQR.SetAbsolutePosition(x, y);
                        content.AddImage(textoImagenQR);
                    }
                    stamper.Close();
                }
            }
        }
    }

    static void AgregarMarcaDeAgua()
    {
        float angulo = 45;
        float opacidad = 0.3f;
        string texto = utiles.marcaAgua;

        // Configurar fuente y tamaño del texto
        BaseFont font = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
        float altoFuente = 25;
        float anchoTexto = 700;
        // Dividir el texto en líneas
        List<string> lineas = utiles.DividirTextoEnLineas(texto, font, altoFuente, anchoTexto);


        using(PdfReader readerPdf = new PdfReader(utiles.PDFEntrada))
        {
            using(FileStream fs = new FileStream(utiles.PDFSalida, FileMode.Create))
            {
                using(PdfStamper stamper = new PdfStamper(readerPdf, fs))
                {
                    //Posicion X e Y donde insertar la marca de agua
                    float posicionX = readerPdf.GetPageSize(1).Width / 2;
                    float posicionY = readerPdf.GetPageSize(1).Height / 2;

                    //Total de paginas del PDF
                    int totalPaginas = stamper.Reader.NumberOfPages;

                    //Se inserta la imagen en todas las paginas
                    for(int i = 1; i <= totalPaginas; i++)
                    {
                        // Obtener la capa de contenido
                        PdfContentByte content = stamper.GetOverContent(i); // Primera página (cambiar índice si es necesario)

                        // Configurar transparencia
                        PdfGState gState = new PdfGState
                        {
                            FillOpacity = opacidad, // Opacidad del texto (0.0 a 1.0)
                            StrokeOpacity = opacidad
                        };

                        content.SaveState(); // Guardar el estado actual
                        content.SetGState(gState);

                        // Configurar fuente y color del texto
                        //BaseFont font = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        content.SetFontAndSize(font, altoFuente); // Tamaño de la fuente
                        content.SetColorFill(BaseColor.GRAY); // Color gris para mejor visibilidad

                        // Rotar y posicionar el texto
                        content.BeginText();
                        //content.ShowTextAligned(PdfContentByte.ALIGN_CENTER, texto, posicionX, posicionY, angulo);

                        float interlineado = altoFuente + 5; // Espaciado entre líneas
                        float offsetY = (lineas.Count - 1) * interlineado / 2; // Para centrar verticalmente

                        //Genera cada linea del texto a insertar
                        for(int j = 0; j < lineas.Count; j++)
                        {
                            string linea = lineas[lineas.Count - 1 - j]; // Recorremos de atrás hacia adelante

                            // Calculamos la posición Y para esta línea
                            float y = posicionY - offsetY + j * interlineado;

                            // Inserta la línea con rotación
                            content.ShowTextAligned(PdfContentByte.ALIGN_CENTER, linea, posicionX, y, angulo);
                        }

                        content.EndText();

                        content.RestoreState(); // Restaurar el estado anterior
                    }

                    stamper.Close();
                }
            }
        }
    }
}
