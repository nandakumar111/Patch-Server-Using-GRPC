using System.Xml.Serialization;

namespace Sender.Models
{
    [XmlRoot(ElementName = "ShareDetails", IsNullable=true)]
    public class ShareDetails
    {
        [XmlElement("FileName")]
        public string FileName { get; set; }
        
        [XmlElement("SenderFilePath")]
        public string SenderFilePath { get; set; }
        
        [XmlElement("ReceiverFilePath")]
        public string ReceiverFilePath { get; set; }
    }
}
