using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SharedCommon
{
    // Need code in here at some point to support the concept of a collection of ReelResources
    // instead of doing it singly, but for now that's beyond my understanding
    [Serializable]
    [XmlRoot("ResourcePresentationList", Namespace ="http://www.smptera.org/schemas/430-11/2010/RPL")]
    public class ResourcePresentationList
    {
        [XmlElement ("ReelResources")]
        public ReelResources ReelResources { get; set; }

        [XmlAttribute ("PlayoutID")]
        public UInt32 PlayoutId { get; set; }

        public ResourcePresentationList()
        {
            PlayoutId = RplPlayoutId.GenerateNewId();
            ReelResources = new ReelResources();
        }
    }

    [Serializable]
    public class ReelResources
    {
        private UInt64 _rateNumerator;
        private UInt64 _rateDenominator;
        private String _editRate;

          
        [XmlElement ("ReelResource")]
        public ReelResource ReelResource { get; set; }

        // As far as I can tell this is some arbitrary GUID that is needed so we generate it randomly below
        [XmlAttribute ("ReelID")]
        public String ReelId { get; set; }

        /// <summary>
        /// Attribute that MUST be a string in the format of Numerator and Denominator, for example: "24 1" 
        /// </summary>
        [XmlAttribute ("EditRate")]
        public String EditRate
        {
            get
            {
                return _editRate;
            }
            set
            {
                this._editRate = value;

                if (this._editRate == null)
                {
                    throw new NullReferenceException("Error: EditRate should never be allowed/set to null - this will mess up derivative calculations");
                }

                // do some logic for parsing out and giving the Numerator and Denominator values
                var SplitString = this._editRate.Split(' ');

                // If it is properly formatted, then we can parse out the rateNumerator and rateDenominator
                if (SplitString.Length == 2)
                {
                    this._rateNumerator = UInt64.Parse(SplitString[0]);
                    this._rateDenominator = UInt64.Parse(SplitString[1]);
                }
            }
        }

        [XmlAttribute ("TimelineOffset")]
        public UInt32 TimelineOffset { get; set; }

        public ReelResources()
        {
            this.ReelResource = new ReelResource();
            this.ReelId = "urn:uuid:" + Guid.NewGuid().ToString();
            this.EditRate = "";
            this.TimelineOffset = 0;
            this._rateNumerator = 0;
            this._rateDenominator = 0;
        }

        public UInt64 RateNumerator
        {
            get
            {
                return _rateNumerator;
            }
        }

        public UInt64 RateDenominator
        {
            get
            {
                return _rateDenominator;
            }
        }
    }

    [Serializable]
    public class ReelResource
    {
        [XmlElement ("ResourceFile")]
        public ResourceFile ResourceFile { get; set; }

        [XmlAttribute ("Language")]
        public String Language { get; set; }

        [XmlAttribute ("Duration")]
        public UInt64 Duration { get; set; }

        [XmlAttribute ("EntryPoint")]
        public UInt64 EntryPoint { get; set; }

        [XmlAttribute ("ResourceType")]
        public String ResourceType { get; set; }

        [XmlAttribute ("Id")]
        public String Id { get; set; }

        [XmlAttribute ("IntrinsicDuration")]
        public UInt64 IntrinsicDuration { get; set; }

        public ReelResource()
        {
            this.ResourceFile = new ResourceFile();
            this.Language = "en-us";  // US english by default, but obviously can be overwritten
            this.Duration = 0;
            this.EntryPoint = 0;
            this.ResourceType = "ClosedCaption"; // defaulting to ClosedCaption, but obviously can be overwritten
            this.Id = "";
            this.IntrinsicDuration = 0;
        }
    }

    [Serializable]
    public class ResourceFile
    {
        [XmlText]
        public String ResourceText { get; set; }

        public ResourceFile()
        {
            this.ResourceText = "";
        }
    }
}
