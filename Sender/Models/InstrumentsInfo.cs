using System;
using System.Xml.Serialization;

namespace Sender.Models
{
    [XmlRoot(ElementName = "InstrumentsInfo", IsNullable=true)]
    public class InstrumentInfo
    {
        [XmlElement("Instruments")]
        public Instruments Instruments { get; set; }
    }

    public class Instruments
    {
        [XmlElement("Instrument")]
        public InstrumentData[] Instrument { get; set; }
    }

    public class InstrumentData
    {
        [XmlElement("NAME")]
        public string Name { get; set; }
        
        [XmlElement("IP")]
        public string IpAddress { get; set; }
        
        [XmlElement("PROTOCOL")]
        public string Protocol { get; set; }

        [XmlElement("PORT")]
        public string Port { get; set; }

        [XmlElement("TYPE")]
        public string Type { get; set; }

        [XmlElement("SERIAL_NO")]
        public string SerialNo { get; set; }

        [XmlElement("PRIMARY")]
        public bool Primary { get; set; }

    }
}
