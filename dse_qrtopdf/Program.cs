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
                Margin = 0
            }
        };

        Bitmap qrImage = writer.Write(utiles.textoQR);

        //Seleccion del tipo de fuente
        System.Drawing.Font font = new System.Drawing.Font("Arial", 8, FontStyle.Bold);
        Brush pincel = Brushes.Black;
        int altoFuente = font.Height * 96 / 72;

        //Medidas del bitmap
        int anchoBitmap = qrImage.Width;
        int altoBitmap = qrImage.Height + (altoFuente);

        //int tamañoBitmap = utiles.tamañoQRPx + (font.Height * 96 / 72); //Se añade la altura de la fuente para que pueda ponerse el texto superior e inferior

        //Bitmap bitmapConTexto = new Bitmap(tamañoBitmap, tamañoBitmap);
        Bitmap bitmapConTexto = new Bitmap(anchoBitmap, altoBitmap);

        using(Graphics graphics = Graphics.FromImage(bitmapConTexto))
        {
            graphics.Clear(Color.White);

            // Dibujar el texto encima
            string texto1 = "QR Tributario";
            SizeF tamañoTextoEncima = graphics.MeasureString(texto1, font);
            float posicionXEncima = (anchoBitmap - tamañoTextoEncima.Width) / 2; // Centrar el texto
            graphics.DrawString(texto1, font, pincel, posicionXEncima, 0);

            // Dibujar el código QR
            graphics.DrawImage(qrImage, 0, (bitmapConTexto.Height - qrImage.Height), qrImage.Width, qrImage.Height);

            // Dibujar el texto debajo
            string texto2 = "VERI*FACTU";
            SizeF tamañoTextoDebajo = graphics.MeasureString(texto2, font);
            float posicionXDebajo = (anchoBitmap - tamañoTextoDebajo.Width) / 2; // Centrar el texto
            graphics.DrawString(texto2, font, pincel, posicionXDebajo, (altoBitmap - altoFuente));


            ////StringFormat formatoTexto = new StringFormat
            ////{
            ////    Alignment = StringAlignment.Center
            ////};

            ////// Calcular la posición para centrar la imagen QR
            ////int posicionX = (tamañoBitmap - utiles.tamañoQRPx) / 2;
            ////int posicionY = tamañoBitmap - utiles.tamañoQRPx;

            ////// Dibujar la imagen del QR en el centro
            ////graphics.DrawImage(qrImage, posicionX, posicionY, utiles.tamañoQRPx, utiles.tamañoQRPx);

            ////// Dibujar el texto superior
            ////graphics.DrawString("QR tributario", font, Brushes.Black, new RectangleF(0, 0, tamañoBitmap, font.Height), formatoTexto);

            ////int coordenadaY = tamañoBitmap - (font.Height * 96 / 72);
            ////// Dibujar el texto inferior
            ////graphics.DrawString("VERI*FATU", font, Brushes.Black, new RectangleF(0, coordenadaY, tamañoBitmap, font.Height), formatoTexto);
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

                    // Convertir las coordenadas de milímetros a puntos (1mm = 2.83465 puntos)
                    float x = utiles.convierteAPx(utiles.margen);
                    if (utiles.alineacion == "DERECHA") x = 595 - imagenQR.Width - utiles.convierteAPx(utiles.margen);

                    float y = (float)(reader.GetPageSize(1).Height - utiles.convierteAPx(utiles.coordenadaY) - imagenQR.Height);


                    // Convertir Bitmap en iTextSharp imagen
                    using(var stream = new MemoryStream())
                    {
                        imagenQR.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        Image textoImagenQR = Image.GetInstance(stream.ToArray());
                        textoImagenQR.SetAbsolutePosition(x, y);
                        textoImagenQR.ScaleAbsolute(imagenQR.Width, imagenQR.Height);  // Ajusta el tamaño de la imagen
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
        string texto = $"DOCUMENTO BORRADOR (sin validez legal)";
        float altoFuente = 30;

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
                        BaseFont font = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        content.SetFontAndSize(font, altoFuente); // Tamaño de la fuente
                        content.SetColorFill(BaseColor.GRAY); // Color gris para mejor visibilidad
                        
                        // Rotar y posicionar el texto
                        content.BeginText();

                        content.ShowTextAligned(PdfContentByte.ALIGN_CENTER, texto, posicionX, posicionY, angulo);
                        content.EndText();

                        content.RestoreState(); // Restaurar el estado anterior
                    }

                    stamper.Close();
                }
            }
        }
    }
}
