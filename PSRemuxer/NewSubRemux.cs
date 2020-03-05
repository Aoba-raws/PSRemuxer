using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;

namespace PSRemuxer
{
    [Cmdlet(VerbsCommon.New, "SubRemux")]
    [Alias("muxsub")]
    public class NewSubRemux
    {
        [Parameter(ParameterSetName = "AutoDetect",
            ValueFromPipeline = true,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty()]
        public string[] LiteralPath { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string OutputPath { get; set; }

        [Parameter(ParameterSetName = "ManuallyDetectWithSubtitle",
            Mandatory = true)]
        [Parameter(ParameterSetName = "ManuallyDetectWithVideo",
            Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string VideoSourceDirectory { get; set; }

        [Parameter(ParameterSetName = "ManuallyDetectWithSubtitle",
            Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string SubtitleSourceDirectory { get; set; }

        [Parameter(ParameterSetName = "ManuallyDetectWithVideo",
            Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string SubtitleVideoSourceDirectory { get; set; }



    }
}
