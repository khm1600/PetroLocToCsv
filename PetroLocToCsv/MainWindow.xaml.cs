using Microsoft.Win32;

using System.IO;
using System.Text;
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

namespace PetroLocToCsv;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        timer.Tick += flushFinishSymbol;
    }

    string? filePath;
    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();
        bool? result = dialog.ShowDialog();
        if (result is true)
        {
            saveButton.IsEnabled = true;
            filePath = dialog.FileName;
            sourcePath.Text = filePath;
        }
        else
        {
            saveButton.IsEnabled = false;
            filePath = null;
            sourcePath.Text = "";
        }
    }

    DispatcherTimer timer = new() { Interval = new(0, 0, 0, 1) };
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new SaveFileDialog();
        picker.DefaultExt = ".csv";
        var result = picker.ShowDialog();
        if (result is true)
        {
            try
            {
                using var stream = File.OpenWrite(picker.FileName);
                var sw = new StreamWriter(stream);
                sw.Write(Extractor.LocFileToCsv(filePath!));
                sw.Flush();
                finish.Visibility = Visibility.Visible;
                timer.Start();
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }

    void flushFinishSymbol(object? _1, object _2)
    {
        finish.Visibility = Visibility.Collapsed;
        timer.Stop();
    }
}

static class Extractor
{
    public static string LocFileToCsv(string path)
    {
        var entries = LoadFile(path);

        var result = new StringBuilder();
        result.AppendLine("\"Text ID\",\"Text\"");

        foreach (var item in entries)
        {
            result.AppendLine($"\"{item.entryTag}\",\"{item.entry.Replace("\"", "\"\"")}\"");
        }
        return result.ToString();
    }

    static Entry[] LoadFile(string path)
    {
        using var s = File.OpenRead(path);

        var count = ReadUInt32(s);

        var result = new Entry[count];

        for (int i = 0; i < count; i++)
        {
            result[i].magic = ReadUInt32(s);
            result[i].entryLength = ReadUInt32(s);
            result[i].entryTagLength = ReadUInt32(s);
        }

        for (int i = 0; i < count; i++)
        {
            var length = result[i].entryLength;
            var sb = new StringBuilder((int)length);
            for (int j = 0; j < length; j++)
            {
                var ch = ReadUInt16(s);
                _ = sb.Append((char)ch);
            }
            result[i].entry = sb.ToString();
        }

        for (int i = 0; i < count; i++)
        {
            var length = result[i].entryTagLength;
            var sb = new StringBuilder((int)length);
            for (int j = 0; j < length; j++)
            {
                var ch = s.ReadByte();
                _ = sb.Append((char)ch);
            }
            result[i].entryTag = sb.ToString();
        }

        return result;

        static uint ReadUInt32(Stream s)
        {
            uint result = 0;

            for (int i = 0; i < 4; i++)
            {
                var b = s.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                result |= (uint)(b & 0xFF) << (i * 8);
            }

            return result;
        }

        static ushort ReadUInt16(Stream s)
        {
            ushort result = 0;

            for (int i = 0; i < 2; i++)
            {
                var b = s.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                result |= (ushort)((b & 0xFF) << (i * 8));
            }

            return result;
        }
    }
}
struct Entry
{
    public uint magic;
    public uint entryLength;
    public uint entryTagLength;
    public string entry { get; set; }
    public string entryTag { get; set; }
}
