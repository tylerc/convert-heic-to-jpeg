using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HeicToJpegUwp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async Task ConvertFile(Windows.Storage.StorageFolder folder, Windows.Storage.StorageFile inputFile)
        {
            try
            {
                // var inputFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                // var folder = await inputFile.GetParentAsync();
                var inputStream = await inputFile.OpenReadAsync();
                var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(inputStream);
                if (decoder.DecoderInformation.CodecId.Equals(Windows.Graphics.Imaging.BitmapDecoder.JpegDecoderId))
                {
                    Debug.WriteLine(inputFile.Name + " is already a JPEG.");

                    // Already jpeg, nothing to do here, except maybe rename the file.
                    if (!inputFile.FileType.EndsWith("jpg") && !inputFile.FileType.EndsWith("jpeg"))
                    {
                        var newFileName = inputFile.Name.Replace(inputFile.FileType + "$", ".jpg");
                        await inputFile.RenameAsync(newFileName);
                        textBox.Text += inputFile.Name + " has been renamed.\n";
                    }

                    return;
                }

                var bitmap = await decoder.GetSoftwareBitmapAsync();

                var outputFileName = inputFile.Name.Replace(inputFile.FileType, "") + ".jpg";
                var outputFileExisting = await folder.TryGetItemAsync(outputFileName);
                if (outputFileExisting != null)
                {
                    textBox.Text += "Output file " + outputFileName + " already exists!\n";
                    return;
                }
                var outputFile = await folder.CreateFileAsync(outputFileName);
                var outputStream = await outputFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                var encoder =
                    await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
                        Windows.Graphics.Imaging.BitmapEncoder.JpegEncoderId,
                        outputStream
                    );
                encoder.SetSoftwareBitmap(bitmap);
                encoder.IsThumbnailGenerated = true;

                await encoder.FlushAsync();
                textBox.Text += "Successfully converted " + inputFile.Name + " to " + outputFileName + "\n";
            }
            catch (Exception e)
            {
                textBox.Text += "Failed to convert file " + inputFile.Name + "\n";
                Debug.WriteLine("Failed to convert file. Exception was:");
                Debug.WriteLine(e);
            }
        }

        private async Task RecurseFolder(Windows.Storage.StorageFolder folder)
        {
            textBox.Text += "Processing folder " + folder.Name + "\n";
            var items = await folder.GetItemsAsync();
            foreach (var item in items)
            {
                if (item.IsOfType(Windows.Storage.StorageItemTypes.File))
                {
                    var file = await folder.GetFileAsync(item.Name);
                    await ConvertFile(folder, file);
                }
                else if (item.IsOfType(Windows.Storage.StorageItemTypes.Folder))
                {
                    textBox.Text += "Found file " + item.Name + "\n";
                    var subFolder = await folder.GetFolderAsync(item.Name);
                    await RecurseFolder(subFolder);
                }
            }
        }

        private async Task PickFolder()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".heic");
            Windows.Storage.StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                textBox.Text += "Searching for HEIC files...\n";
                var start = DateTime.Now;
                await RecurseFolder(folder);
                var end = DateTime.Now;
                textBox.Text += "Complete! Time took: " + Math.Round((end - start).TotalSeconds) + " seconds.";
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            textBox.Text = "Messages about the conversion process will appear here.\n";
            PickFolder();
        }
    }
}
