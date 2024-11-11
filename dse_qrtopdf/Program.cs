using System;
using System.Drawing;
using System.IO;
using iTextSharp.text.pdf;
using Image = iTextSharp.text.Image;
using dse_qrtopdf;
using ZXing;

class Program
{
   
    static Utilidades utiles = new Utilidades();

    static void Main(string[] argumentos)
    {
        //Carga los datos del guion
        if (argumentos.Length > 0)
        {
            utiles.CargaGuion(argumentos[0]);
        }
        else
        {
            File.WriteAllText("errores.txt", "No se ha pasado el guion");
            Environment.Exit(0);
        }

        ////// Genera el código QR como una imagen Bitmap
        ////Bitmap imagenQR = GenerarCodigoQR(utiles.textoQR, (int)(utiles.anchoQR * dpi / pulgadas));

        //Genera el QR (tamaño en mm)
        var imagenQR = GeneradorQR();

        // Crear el PDF e insertar la imagen QR
        InsertaQRenPDF(imagenQR);
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
                Margin = utiles.margen
            }
        };


        Font font = new Font("Arial", 10, FontStyle.Bold);
        int tamañoBitmap = utiles.tamañoQRPx + (utiles.margenPx * 2) + (font.Height * 2);

        Bitmap qrImage = writer.Write(utiles.textoQR);
        Bitmap bitmapConTexto = new Bitmap(tamañoBitmap, tamañoBitmap);

        using (Graphics graphics = Graphics.FromImage(bitmapConTexto))
        {
            graphics.Clear(Color.White);

            StringFormat formatoTexto = new StringFormat
            {
                Alignment = StringAlignment.Center
            };

            // Dibujar la imagen del QR en el centro
            graphics.DrawImage(qrImage, utiles.margenPx, font.Height, utiles.tamañoQRPx, utiles.tamañoQRPx);

            // Dibujar el texto superior
            graphics.DrawString("QR tributario", font, Brushes.Black, new RectangleF(0, 0, tamañoBitmap, font.Height), formatoTexto);

            // Dibujar el texto inferior
            graphics.DrawString("VERI*FATU", font, Brushes.Black, new RectangleF(0, utiles.tamañoQRPx + font.Height + utiles.margenPx, tamañoBitmap, font.Height), formatoTexto);
        }


        return bitmapConTexto;
    }


    static void InsertaQRenPDF(Bitmap imagenQR)
    {
        using (PdfReader reader = new PdfReader(utiles.PDFEntrada))
        {
            using (FileStream fs = new FileStream(utiles.PDFSalida, FileMode.Create))
            {
                using (PdfStamper stamper = new PdfStamper(reader, fs))
                {
                    // Obtener la primera página del PDF 
                    PdfContentByte content = stamper.GetOverContent(1); // Índice de la página (1 es la primera página)

                    // Convertir las coordenadas de milímetros a puntos (1mm = 2.83465 puntos)
                    float x = utiles.convierteAPx(utiles.coordenadaX);
                    float y = (float)(reader.GetPageSize(1).Height - utiles.convierteAPx(utiles.coordenadaY) - imagenQR.Height);


                    // Convertir Bitmap en iTextSharp imagen
                    using (var stream = new MemoryStream())
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
}
