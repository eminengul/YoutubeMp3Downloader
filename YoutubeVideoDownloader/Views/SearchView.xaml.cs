using System;
using System.Collections.Generic;
using System.Linq;
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
using YoutubeExplode;
using YoutubeExplode.Common;
using NAudio.Lame;
using NAudio.Wave;
using System.IO;
using YoutubeExplode.Videos.Streams;
using System.Windows.Threading;
using YoutubeVideoDownloader.Models;
using System.Web.UI.WebControls;
using System.Threading;
using System.Web.Configuration;

namespace YoutubeVideoDownloader.Views
{
	
	public partial class SearchView : UserControl
	{
		private DispatcherTimer timer;
		private int progressValue = 0;

		public SearchView()
		{
			InitializeComponent();
			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(100);
			timer.Tick += Timer_Tick;
		}
		private void Timer_Tick(object sender, EventArgs e)
		{
			progressValue++;
			if (progressValue > 100)
			{
				progressValue = 0;
			}
			CircleProgressBar.Value = progressValue;
		}
		private async void OnSearchButtonClick(object sender, RoutedEventArgs e)
		{
			string query = SearchTextBox.Text;
			if (!string.IsNullOrWhiteSpace(query))
			{
				await SearchYoutubeVideosAsync(query);
			}
		}
		private void ShowLoadingView()
		{
			progressValue = 0; 
			CircleProgressBar.Visibility = Visibility.Visible;
			timer.Start();
			SearchTextBox.IsEnabled = false;
			ResultsListView.IsEnabled = false;
		}

		private void HideLoadingView()
		{
			progressValue = 0; 
			CircleProgressBar.Visibility = Visibility.Collapsed;
			timer.Stop();
			SearchTextBox.IsEnabled = true;
			ResultsListView.IsEnabled = true;
		}
		private async Task SearchYoutubeVideosAsync(string query)
		{
			ShowLoadingView();
			var youtube = new YoutubeClient();
			var searchResults = await youtube.Search.GetVideosAsync(query).CollectAsync(50);

			var videoList = searchResults.Select(video => new VideoResult
			{
				UrlFoto = video.Thumbnails.FirstOrDefault()?.Url,
				Title = video.Title,
				Author = video.Author.Title,
				UploadDate = video.Duration.ToString(),
				Url = $"https://www.youtube.com/watch?v={video.Id}"
			}).ToList();

			ResultsListView.ItemsSource = videoList;
			HideLoadingView();
		}
		private async void OnDownloadButtonClick(object sender, RoutedEventArgs e)
		{
			ShowLoadingView() ;
			var button = sender as System.Windows.Controls.Button;
			if (button != null)
			{
				var videoResult = button.DataContext as VideoResult;
				if (videoResult != null)
				{
				
					try
					{
						var youtube = new YoutubeClient();
						var video = await youtube.Videos.GetAsync(videoResult.Url);
						var streamManifest =  await youtube.Videos.Streams.GetManifestAsync(video.Id);
						var audioStreamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate();
						var stream = await youtube.Videos.Streams.GetAsync(audioStreamInfo);

						string tempAudioFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{video.Title}.{audioStreamInfo.Container}");
						using (var fileStream = new FileStream(tempAudioFilePath, FileMode.Create, FileAccess.Write))
						{
							await stream.CopyToAsync(fileStream);
						}
						string tempMp3FilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{video.Title}.mp3");
						string tempWavFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{video.Title}.wav");

						using (var mediaFoundationReader = new MediaFoundationReader(tempAudioFilePath))
						{
							WaveFileWriter.CreateWaveFile(tempWavFilePath, mediaFoundationReader);
						}
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
						if (!Directory.Exists($"{baseDirectory}\\Muzik"))
						{
							Directory.CreateDirectory($"{baseDirectory}\\Muzik");
						}
                        string outputPath = $"{baseDirectory}\\Muzik\\{video.Title}.mp3";
						using (var reader = new AudioFileReader(tempWavFilePath))
						using (var writer = new LameMP3FileWriter(outputPath, reader.WaveFormat, LAMEPreset.VBR_90))
						{
							reader.CopyTo(writer);
						}

						File.Delete(tempAudioFilePath);
						File.Delete(tempWavFilePath);

						HideLoadingView();
						MessageBox.Show($"Video başarıyla MP3 olarak indirildi: {outputPath}");
					}
					catch (Exception ex)
					{
						HideLoadingView();
						MessageBox.Show($"Bir hata oluştu: {ex.Message}");
					}
				}
			}

		}

		private async void OnPlayedButtonClick(object sender, RoutedEventArgs e)
		{
			var button = sender as System.Windows.Controls.Button;
			var videoResult = button.DataContext as VideoResult;
			if (videoResult != null)
			{
				var youtube = new YoutubeClient();
				var video = await youtube.Videos.GetAsync(videoResult.Url);
				var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
				var audioStreamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate();
				var stream = await youtube.Videos.Streams.GetAsync(audioStreamInfo);

				string tempAudioFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{video.Title}.{audioStreamInfo.Container}");
				using (var fileStream = new FileStream(tempAudioFilePath, FileMode.Create, FileAccess.Write))
				{
					await stream.CopyToAsync(fileStream);
				}
				//string tempMp3FilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{video.Title}.mp3");
				//string tempWavFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{video.Title}.wav");

				//using (var mediaFoundationReader = new MediaFoundationReader(tempAudioFilePath))
				//{
				//	WaveFileWriter.CreateWaveFile(tempWavFilePath, mediaFoundationReader);
				//}
				playVideo.Visibility = Visibility.Visible;
				mediaPlayer.Source = new Uri(tempAudioFilePath); 
				mediaPlayer.Play();
				Thread.Sleep(10000);
				File.Delete(tempAudioFilePath);
			}
		}



	}
}
