using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;

namespace PSRemuxer
{
    [Cmdlet(VerbsCommon.New, "SubRemux")]
    [Alias("muxsub")]
    public class NewSubRemux : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string OutputPath { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string VideoSourceDirectory { get; set; }

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        public string VideoSourcePattern { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string OutputVideoTrackTitle { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string OutputSubtitleTrackTitle { get; set; }

        [Parameter(ParameterSetName = "ManuallyDetectWithSubtitle",
            Mandatory = true)]
        [Parameter(ParameterSetName = "ManuallyDetectWithVideo")]
        [ValidateNotNullOrEmpty()]
        public string SubtitleSourceDirectory { get; set; }

        [Parameter(ParameterSetName = "ManuallyDetectWithSubtitle")]
        [Parameter(ParameterSetName = "ManuallyDetectWithVideo")]
        [ValidateNotNullOrEmpty()]
        public string SubtitleSourcePattern { get; set; }

        [Parameter(ParameterSetName = "ManuallyDetectWithVideo",
            Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string SubtitleVideoSourceDirectory { get; set; }

        [Parameter(ParameterSetName = "ManuallyDetectWithVideo")]
        [ValidateNotNullOrEmpty()]
        public string SubtitleVideoSourcePattern { get; set; }

        protected override void BeginProcessing()
        {
            var videoSourcePath = SessionState.Path.GetResolvedPSPathFromPSPath(VideoSourceDirectory)?[0].Path;
            string subtitleVideoSourcePath = "";
            string subtitleSourcePath = "";
            bool isUseSubtitle = string.IsNullOrEmpty(SubtitleSourceDirectory);
            bool isUseSubtitleVideo = string.IsNullOrEmpty(SubtitleVideoSourceDirectory);
            if (isUseSubtitle)
            {
                subtitleSourcePath = SessionState.Path.GetResolvedPSPathFromPSPath(SubtitleSourceDirectory)?[0].Path;
            }

            if (isUseSubtitleVideo)
            {
                subtitleVideoSourcePath = SessionState.Path.GetResolvedPSPathFromPSPath(SubtitleVideoSourceDirectory)?[0].Path;
            }

            var videoFileInfos = (new DirectoryInfo(videoSourcePath))
                .GetFiles()
                .Where(fi => string.IsNullOrWhiteSpace(VideoSourcePattern)
                    ? Regex.IsMatch(fi.Extension, "mp4|mkv|avi|ts|mov|vob", RegexOptions.IgnoreCase)
                    : Regex.IsMatch(fi.Name, videoSourcePath))
                .ToArray();

            FileInfo[] subtitleFileInfos = null;
            FileInfo[] subtitleVideoFileInfos = null;

            if (isUseSubtitle)
            {
                subtitleFileInfos = (new DirectoryInfo(subtitleSourcePath))
                    .GetFiles()
                    .Where(fi => string.IsNullOrWhiteSpace(subtitleSourcePath)
                        ? Regex.IsMatch(fi.Extension, "ass|ssa|srt|sub|sup", RegexOptions.IgnoreCase)
                        : Regex.IsMatch(fi.Name, subtitleSourcePath))
                    .ToArray();
            }

            if (isUseSubtitleVideo)
            {
                subtitleVideoFileInfos = (new DirectoryInfo(videoSourcePath))
                    .GetFiles()
                    .Where(fi => string.IsNullOrWhiteSpace(VideoSourcePattern)
                        ? Regex.IsMatch(fi.Extension, "mp4|mkv|avi|ts|mov|vob", RegexOptions.IgnoreCase)
                        : Regex.IsMatch(fi.Name, videoSourcePath))
                    .ToArray();
            }

            if (!(isUseSubtitle
                ? isUseSubtitleVideo
                    ? subtitleVideoFileInfos.Length == videoFileInfos.Length && subtitleFileInfos.Length == videoFileInfos.Length
                    : subtitleFileInfos.Length == videoFileInfos.Length
                : subtitleFileInfos.Length == videoFileInfos.Length))
            {
                WriteError(new ErrorRecord(new ItemNotFoundException("Subtitle or SubtitleVideo number not match video number."), "0", ErrorCategory.ResourceUnavailable, videoFileInfos));
                return;
            }

            //extract ass
            if (isUseSubtitleVideo && !isUseSubtitle)
            {
                ConversionQueue queue = new ConversionQueue(false);
                for (int i = 0; i < subtitleVideoFileInfos.Length; i++)
                {
                    var mediainfo = MediaInfo.Get(subtitleVideoFileInfos[i]).GetAwaiter().GetResult();
                    if (mediainfo.SubtitleStreams.Count() <= 0)
                    {
                        WriteError(new ErrorRecord(new ItemNotFoundException("Cannot found subtitle in video file."), "0", ErrorCategory.ResourceUnavailable, subtitleVideoFileInfos));
                        return;
                    }
                    queue.Add(new Conversion()
                        .AddStream(mediainfo.SubtitleStreams.First())
                        .SetOutput(SessionState.Path.GetUnresolvedProviderPathFromPSPath(subtitleVideoFileInfos[i].Name + ".PSRemux.ass")));
                }
                queue.OnConverted += (c, t, d) => WriteProgress(new ProgressRecord(1, "Extracting Subtitle", "") { PercentComplete = (int)((float)c / t * 100) });
                queue.Start();
                isUseSubtitle = true;
                subtitleFileInfos = (new DirectoryInfo(subtitleVideoSourcePath))
                    .GetFiles()
                    .Where(fi => Regex.IsMatch(fi.Name, @"\.PSRemux\.ass", RegexOptions.IgnoreCase))
                    .ToArray();
            }

            //use sushi to currect timeline
            if (isUseSubtitle && isUseSubtitleVideo)
            {
                for (int i = 0; i < subtitleVideoFileInfos.Length; i++)
                {
                    Process sushiProcess = new Process();
                    sushiProcess.StartInfo = new ProcessStartInfo()
                    {
                        FileName = "sushi",
                        Arguments = $"--script {subtitleFileInfos[i].FullName} --src {subtitleVideoFileInfos[i].FullName} --dst {videoFileInfos[i].FullName} -o {videoFileInfos[i].Name}.PSRemuxSushi.ass",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    sushiProcess.Start();
                    sushiProcess.WaitForExit();
                    WriteProgress(new ProgressRecord(1, "Extracting Subtitle", "") { PercentComplete = (int)((float)i / subtitleVideoFileInfos.Length * 100) });
                }
                foreach (var item in subtitleFileInfos)
                {
                    item.Delete();
                }
                subtitleFileInfos = (new DirectoryInfo(subtitleVideoSourcePath))
                    .GetFiles()
                    .Where(fi => Regex.IsMatch(fi.Name, @"\.PSRemuxSushi\.ass", RegexOptions.IgnoreCase))
                    .ToArray();
            }

            //remux to mkv
            for (int i = 0; i < subtitleVideoFileInfos.Length; i++)
            {
                Process mkvmergeProcess = new Process();
                mkvmergeProcess.StartInfo = new ProcessStartInfo()
                {
                    FileName = "mkvmerge",
                    Arguments = $"-o \"{SessionState.Path.GetUnresolvedProviderPathFromPSPath(OutputPath + "\\" + videoFileInfos[i].Name)}\" --track-name 0:\"{OutputVideoTrackTitle}\" \"{videoFileInfos[i].FullName}\" --track-name 0:\"{OutputSubtitleTrackTitle}\" \"{subtitleFileInfos[i].FullName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                mkvmergeProcess.Start();
                mkvmergeProcess.WaitForExit();
            }
            subtitleFileInfos.Where(fi => Regex.IsMatch(fi.Name, @"\.PSRemuxSushi\.ass", RegexOptions.IgnoreCase)).ToList().ForEach(fi => fi.Delete());

        }
    }
}
