using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLAYGROUND
{
    public class BP
    {
        //private static byte a = 3;
        private static byte r = 2;
        private static byte g = 1;
        private static byte b = 0;

        public static byte[] Invert(byte[] bits)
        {
            int div = 16;
            Parallel.For(0, bits.Length / div, i =>
            {
                for (int j = 0; j < div; j++)
                {
                    if (j % 4 != 3)  // Avoid modifying the alpha channel in RGBA images
                        bits[i * div + j] = (byte)(255 - bits[i * div + j]);
                }
            });
            return bits;
        }

        private static void GrayPixel(byte[] bits, int div, int i, int idx)
        {
            float val;

            val = bits[(i * div) + idx + r] + bits[(i * div) + idx + g] + bits[(i * div) + idx + b];
            val /= 3;

            bits[(i * div) + idx + r] = (byte)val;
            bits[(i * div) + idx + g] = (byte)val;
            bits[(i * div) + idx + b] = (byte)val;
        }

        private static void SepiaPixel(byte[] bits, int div, int i, int idx)
        {
            int newRed;
            int newBlue;
            int newGreen;

            newRed = (int)((bits[(i * div) + idx + r] * 0.393) + (bits[(i * div) + idx + g] * 0.769) + (bits[(i * div) + idx + b] * 0.189));
            newGreen = (int)((bits[(i * div) + idx + r] * 0.349) + (bits[(i * div) + idx + g] * 0.686) + (bits[(i * div) + idx + b] * 0.168));
            newBlue = (int)((bits[(i * div) + idx + r] * 0.272) + (bits[(i * div) + idx + g] * 0.534) + (bits[(i * div) + idx + b] * 0.131));

            newRed = Math.Min(255, newRed);
            newGreen = Math.Min(255, newGreen);
            newBlue = Math.Min(255, newBlue);

            bits[(i * div) + idx + r] = (byte)newRed;
            bits[(i * div) + idx + g] = (byte)newGreen;
            bits[(i * div) + idx + b] = (byte)newBlue;
        }
        private static void BrightnessPixel(byte[] bits, int div, int i, int idx, int value)
        {
            int newRed;
            int newBlue;
            int newGreen;

            newRed = bits[(i * div) + idx + r] + value;
            newGreen = bits[(i * div) + idx + g] + value;
            newBlue = bits[(i * div) + idx + b] + value;

            newRed = Math.Min(255, newRed);
            newGreen = Math.Min(255, newGreen);
            newBlue = Math.Min(255, newBlue);

            bits[(i * div) + idx + r] = (byte)newRed;
            bits[(i * div) + idx + g] = (byte)newGreen;
            bits[(i * div) + idx + b] = (byte)newBlue;
        }

        public static byte[] Gray(byte[] bits)
        {
            int div = 16;
            Parallel.For(0, bits.Length / div, i => // unrolling 
            {
                GrayPixel(bits, div, i, 0);
                GrayPixel(bits, div, i, 4);
                GrayPixel(bits, div, i, 8);
                GrayPixel(bits, div, i, 12);
            });

            return bits;
        }

        public static byte[] Sepia(byte[] bits)
        {
            int div = 16;

            Parallel.For(0, bits.Length / div, i => // unrolling 
            {
                SepiaPixel(bits, div, i, 0);
                SepiaPixel(bits, div, i, 4);
                SepiaPixel(bits, div, i, 8);
                SepiaPixel(bits, div, i, 12);
            });

            return bits;
        }


        private static void BinaryPixel(byte[] bits, int div, int i, int idx, int threshold)
        {
            float val;
            val = bits[(i * div) + idx + r] + bits[(i * div) + idx + g] + bits[(i * div) + idx + b];
            val /= 3;
            if (val < threshold)
                val = 0;
            else
                val = 255;

            bits[(i * div) + idx + r] = (byte)val;
            bits[(i * div) + idx + g] = (byte)val;
            bits[(i * div) + idx + b] = (byte)val;
        }

        public static byte[] Binary(byte[] bits)
        {
            int div = 16;
            Parallel.For(0, bits.Length / div, i => // unrolling 
            {
                BinaryPixel(bits, div, i, 0, 128);
                BinaryPixel(bits, div, i, 4, 128);
                BinaryPixel(bits, div, i, 8, 128);
                BinaryPixel(bits, div, i, 12, 128);
            });

            return bits;
        }
        //Convoluted filter
        public static void ApplyConvolutionalFilter(byte[] bits, int width, int height, int stride, int pixelSize, float[,] depthBuffer)
        {
            float[,] kernel = { { 1f / 9, 1f / 9, 1f / 9 }, { 1f / 9, 1f / 9, 1f / 9 }, { 1f / 9, 1f / 9, 1f / 9 } }; // Normalized kernel
            byte[,] colorBuffer = new byte[height, width * 3]; // RGB format
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    int red = 0, green = 0, blue = 0;
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int px = x + kx;
                            int py = y + ky;
                            if (px >= 0 && px < width && py >= 0 && py < height)
                            {
                                int pixelIndex = (py * stride + px * pixelSize);
                                red += (int)(bits[pixelIndex] * kernel[ky + 1, kx + 1]);
                                green += (int)(bits[pixelIndex + 1] * kernel[ky + 1, kx + 1]);
                                blue += (int)(bits[pixelIndex + 2] * kernel[ky + 1, kx + 1]);
                            }
                        }
                    }
                    int idx = y * stride + x * 3;
                    colorBuffer[y, x * 3] = (byte)Math.Min(Math.Max(red, 0), 255);
                    colorBuffer[y, x * 3 + 1] = (byte)Math.Min(Math.Max(green, 0), 255);
                    colorBuffer[y, x * 3 + 2] = (byte)Math.Min(Math.Max(blue, 0), 255);
                }
            });

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 3;
                    bits[index] = colorBuffer[y, x * 3];
                    bits[index + 1] = colorBuffer[y, x * 3 + 1];
                    bits[index + 2] = colorBuffer[y, x * 3 + 2];
                }
            }
        }
    }
}
