using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace OIMS
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IOIMSService" in both code and config file together.
    [ServiceContract]
    public interface IOIMSService
    {

        [OperationContract]
        string GetData(int value);

        [OperationContract]
        //CompositeType GetDataUsingDataContract(CompositeType composite);
        CompositeType GetDataUsingDataContract();

        [OperationContract]
        // TODO: Add your service operations here
        List<IOTNodeData> GetIOTNodeData();

    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }

    [DataContract]
    public class SensorData
    {
        public string sensorId = new string('c', 20);
        public string value = new string('c', 20);
    }

    [DataContract]
    public class IOTNodeData
    {
        public bool boolValue = true;
        public string strIOTNodeId = "Hello ";

        
        public SensorData[] sensorData;
        public IOTNodeData()
        {
            sensorData = new SensorData[4];
            sensorData[0] = new SensorData();
            sensorData[1] = new SensorData();
            sensorData[2] = new SensorData();
            sensorData[3] = new SensorData();
        }

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StrIOTNodeId
        {
            get { return strIOTNodeId; }
            set { strIOTNodeId = value; }
        }
    }

}
