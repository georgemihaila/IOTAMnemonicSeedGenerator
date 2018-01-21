using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace IOTAWordSeedGenerator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loading_TextBlock.Opacity = 0;
            Canvas.SetLeft(Recover_Canvas, -1000);
            Recovery_TextBlock.MouseEnter += (a, b) =>
            {
                Recovery_TextBlock.Foreground = new SolidColorBrush(Colors.Red);
            };
            Recovery_TextBlock.MouseLeave += (a, b) =>
            {
                Recovery_TextBlock.Foreground = new SolidColorBrush(Colors.Black);
            };
            Recovery_TextBlock.MouseLeftButtonDown += (a, b) =>
            {
                Copied();
                Clipboard.SetText(Recovery_TextBlock.Text.Split('\n')[1]);
            };
        }
        

        private readonly Dispatcher UIDispatcher = Dispatcher.CurrentDispatcher;
        private readonly string wordFileHash = "2AE5C14FE46B439B530B3E8AD756EF041076914C9E38C9DD027C93019604A3B144AAA955D0F832DCDBE00D618B49D3BA7297C07F2E02592632A42BD6C8844642";
        string[] words = new string[0];
        bool recoveryMode = false;

        async private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!recoveryMode)
            try
            {
                GenerateButton.IsEnabled = false;
                Loading_TextBlock.Opacity = 1;
                SHA512Managed sha512 = new SHA512Managed();
                if (words.Length == 0)
                {
                    FileStream fileStream = File.OpenRead("words.txt");
                    byte[] hash = sha512.ComputeHash(fileStream);
                    fileStream.Close();
                    if (wordFileHash != BitConverter.ToString(hash).Replace("-", string.Empty)) throw new Exception("Word file has been altered!");
                    await Task.Run(() =>
                    {

                        string[] thread2Words = File.ReadAllLines("words.txt");
                        UIDispatcher.Invoke(() => { words = thread2Words; });
                    });
                }
                Loading_TextBlock.Text = "Your 12 word seed:";
                SeedWords_Canvas.Children.Clear();
                TextBlock[] seedWords_TextBlock = new TextBlock[12];
                string[] seedWords = new string[12];
                for (int i = 0; i < seedWords_TextBlock.Length; i++)
                {
                    seedWords_TextBlock[i] = new TextBlock();
                    seedWords_TextBlock[i].FontWeight = FontWeights.Bold;
                    seedWords_TextBlock[i].Foreground = new SolidColorBrush(Colors.White);
                    seedWords[i] = words[NextInt(words.Length)].ToLower();
                    seedWords_TextBlock[i].Text = seedWords[i];
                    seedWords_TextBlock[i].MouseEnter += (a, b) =>
                    {
                        (a as TextBlock).Foreground = new SolidColorBrush(Colors.Red);
                    };
                    seedWords_TextBlock[i].MouseLeave += (a, b) =>
                    {
                        (a as TextBlock).Foreground = new SolidColorBrush(Colors.White);
                    };
                    SeedWords_Canvas.Children.Add(seedWords_TextBlock[i]);
                    Canvas.SetLeft(seedWords_TextBlock[i], 5 + (i % 4) * (SeedWords_Canvas.ActualWidth / 4));
                    Canvas.SetTop(seedWords_TextBlock[i], 15 + (i % 3) * 30);
                }
                TextBlock seedTextBlock = new TextBlock();
                seedTextBlock.FontWeight = FontWeights.Bold;
                seedTextBlock.Width = SeedWords_Canvas.ActualWidth - 20;
                seedTextBlock.TextWrapping = TextWrapping.Wrap;
                seedTextBlock.Foreground = new SolidColorBrush(Colors.White);
                string seed = string.Empty;
                for (int i = 0; i < 12; i++)
                    seed += seedWords[i];
                string IOTASeed = string.Empty;
                while (!IOTASeed.Contains("9")) IOTASeed = GenerateSeed(seed);
                seedTextBlock.Text = "Your IOTA seed:\n" + IOTASeed;
                seedTextBlock.MouseEnter += (a, b) =>
                {
                    (a as TextBlock).Foreground = new SolidColorBrush(Colors.Red);
                };
                seedTextBlock.MouseLeave += (a, b) =>
                {
                    (a as TextBlock).Foreground = new SolidColorBrush(Colors.White);
                };
                seedTextBlock.MouseLeftButtonDown += (a, b) =>
                {
                    Clipboard.SetText(IOTASeed);
                    Copied();
                };
                SeedWords_Canvas.Children.Add(seedTextBlock);
                Canvas.SetLeft(seedTextBlock, 5);
                Canvas.SetTop(seedTextBlock, 120);
            }
            catch (Exception ex)
            { 
                Loading_TextBlock.Text = ex.ToString();
            }
            finally
            {
                GenerateButton.IsEnabled = true;
            }
            else
            {
                string seed = Recovery_TextBox.Text.Replace(" ", string.Empty);
                Recovery_TextBlock.Text = "Your IOTA seed:\n" + GenerateSeed(seed);
            }
        }

        RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        private static int NextInt(int max)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[4];

            rng.GetBytes(buffer);
            int result = BitConverter.ToInt32(buffer, 0);

            return new Random(result).Next(0, max);
        }

        private string GenerateSeed(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);
                byte[] seedBytes = new byte[81];
                for (int i = 0; i < 64; i++)
                    seedBytes[i] = hashedInputBytes[i];
                for (int i = 64; i < 81; i++)
                {
                    var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                    foreach (var b in hashedInputBytes)
                        hashedInputStringBuilder.Append(b.ToString("X2"));
                    string seed1hash = hashedInputStringBuilder.ToString();
                    var bytes2 = hash.ComputeHash(Encoding.UTF8.GetBytes(seed1hash));
                    seedBytes[i] = bytes2[i - 64];
                }
                string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ9";
                string seed = string.Empty;
                for (int i = 0; i < 81; i++)
                {
                    seed += charset[seedBytes[i] % charset.Length];
                }
                return seed;
            }
        }

        private async void Copied()
        {
            Loading_TextBlock.Text = "Seed copied to clipbard!";
            await Task.Delay(2000);
            Loading_TextBlock.Text = "Your 12 word seed:";
        }

        private void RecoverButton_Click(object sender, RoutedEventArgs e)
        {
            recoveryMode = true;
            Canvas.SetLeft(Recover_Canvas, 0);
            GenerateButton.Content = "Recover";
            Loading_TextBlock.Opacity = 1;
            Loading_TextBlock.Text = "Enter your 12 words, separated by spaces:";
        }

        private void RecoveryClose_Button_Click(object sender, RoutedEventArgs e)
        {
            Canvas.SetLeft(Recover_Canvas, -1000);
            GenerateButton.Content = "Generate 12 word mnemonic phrase";
            Loading_TextBlock.Text = string.Empty;
            recoveryMode = false;
        }
    }
}
