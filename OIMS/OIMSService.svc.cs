using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Windows.Forms;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Security.Permissions;
using System.Web;
using System.Web.Services;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Xml;
using System.Xml.Serialization;

namespace OIMS
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "OIMSService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select OIMSService.svc or OIMSService.svc.cs at the Solution Explorer and start debugging.
    public class OIMSService : IOIMSService
    {
        public ListOfIOTNodes mListOfIOTNodesConnectedtoWifiIntrfc1;
        private iniFileReader mAvailableInterfaceLAN;
        string[] mIOTLANArray;

        public OIMSService()
        {
            //         FileWrite debugOPfile = new OIMS.FileWrite(Convert.ToString("d:\\OIMS.txt"));
        }

        ~OIMSService()
        {

        }


        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract() 
        {
            CompositeType cmpTyp = new CompositeType();
            cmpTyp.BoolValue = true;
            cmpTyp.StringValue = "venkat";
            return cmpTyp;
        }


        public List<IOTNodeData> GetIOTNodeData()
        {
            int ii = 0;
            var list = new List<IOTNodeData> ();

            mAvailableInterfaceLAN = new iniFileReader();
            mAvailableInterfaceLAN.IniParser("d:\\oimsconfig.ini");
            mIOTLANArray = mAvailableInterfaceLAN.EnumSection("ListofIOTLANnetworks");
            mListOfIOTNodesConnectedtoWifiIntrfc1 = new ListOfIOTNodes();

            //collect latest list of IOT node IP-addressess for each wifi interafce "IP subnet LAN"
            mListOfIOTNodesConnectedtoWifiIntrfc1.refreshConnectedIOTNodeList(mIOTLANArray[0]);

            //collect IOTNode data from each IOT node
            foreach (string ipAddress in mListOfIOTNodesConnectedtoWifiIntrfc1.NodeIPlistHashtable.Keys)
            {
                //form the uri...
                //ipAddress = "10.1.173.98";
                String uriStr = "http://" + "10.1.173.147" + "/IOTNodesvc/IOTNodesvc.svc"; 
                System.Uri uri = new Uri(uriStr);

                //create webservice invoker...
                WebServiceInvoker wsInv = new WebServiceInvoker(uri);

                //enumerate webservice methods...
                List<string> mthdList = new List<string>();
                mthdList = wsInv.EnumerateServiceMethods("IOTNodesvcClient");

                //...print webservice methods of IOTNodesvc
                for (int i = 0; i < mthdList.Count; i++)
                {
                    //Console.WriteLine(mthdList[i]);
                }

                //invoke IOT-node websvc to gather data..
                dynamic cmpTyp1 = new object();
                cmpTyp1 = wsInv.InvokeMethod<dynamic>("IOTNodesvcClient", "GetIotData", null);

                //populate IOT--NodeDEtails 
                IOTNodeData iotNdData = new IOTNodeData();
                iotNdData.strIOTNodeId = cmpTyp1.strIOTNodeId;
                while (ii<4)
                {
                    //Console.WriteLine("Sendor ID:---" + cmpTyp1.sensorData[ii].sensorId);
                    //Console.WriteLine("Sendor value :---" + cmpTyp1.sensorData[ii].value);
                    ii++;

                    iotNdData.sensorData[ii].sensorId = cmpTyp1.sensorData[ii].sensorId;
                    iotNdData.sensorData[ii].value = cmpTyp1.sensorData[ii].value;
                }

                list.Add(iotNdData);

            }//end of loop--each IP-node coonected to wifi interface1


            return list;

        } //end of getIOTNodeData function()


    }//end of OIMS class implemenation


} //end of namespace OIMS



/***
public List<IOTNodeData> GetIOTNodeData()
{

    /***
    IOTNodeData iotNode1Data;// = new IOTNodeData() { boolValue = true, strIOTNodeId = "node1", };
    IOTNodeData iotNode2Data;// = new IOTNodeData() { boolValue = true, strIOTNodeId = "node2", };
    var list = new List<IOTNodeData>();

    //0=-0=-00=-0=-0=-0-=0=-0=-0-=

    System.Net.WebClient client = new System.Net.WebClient();

    //string[] ipAddrList = { "192.168.137.230", "192.168.137.161" };
    //http://localhost:10814/IOTNodesvc.svc?wsdl

    string[] ipAddrList = { "10.1.173.98", "10.1.173.98" };

    for (int i = 0; i < 2; i++)
    {
        string url = "http://" + ipAddrList[i] + ":10814/IOTNodesvc.svc?wsdl";
        //:10814/IOTNodesvc.svc?wsdl
        //System.IO.Stream stream = client.OpenRead("http://192.168.137.230/add1.asmx?wsdl");

        System.IO.Stream stream = client.OpenRead(url);
        // Get a WSDL file describing a service.

        ServiceDescription description = ServiceDescription.Read(stream);

        // Initialize a service description importer.

        ServiceDescriptionImporter importer = new ServiceDescriptionImporter();

        importer.ProtocolName = "Soap12";  // Use SOAP 1.2.
        importer.AddServiceDescription(description, null, null);


        // Report on the service descriptions.
        MessageBox.Show("Importing { 0} service descriptions with { 1} associated schemas." + importer.ServiceDescriptions.Count + importer.Schemas.Count);

        // Generate a proxy client.
        importer.Style = ServiceDescriptionImportStyle.Client;


        // Generate properties to represent primitive values.
        importer.CodeGenerationOptions = System.Xml.Serialization.CodeGenerationOptions.GenerateProperties;

        // Initialize a Code-DOM tree into which we will import the service.
        CodeNamespace nmspace = new CodeNamespace();
        CodeCompileUnit unit1 = new CodeCompileUnit();

        unit1.Namespaces.Add(nmspace);

        // Import the service into the Code-DOM tree. This creates proxy code
        // that uses the service.

        ServiceDescriptionImportWarnings warning = importer.Import(nmspace, unit1);
        if (warning == 0)
        {
            // Generate and print the proxy code in C#.
            CodeDomProvider provider1 = CodeDomProvider.CreateProvider("CSharp");
            // Compile the assembly with the appropriate references
            string[] assemblyReferences = new string[2] { "System.Web.Services.dll", "System.Xml.dll" };
            CompilerParameters parms = new CompilerParameters(assemblyReferences);
            CompilerResults results = provider1.CompileAssemblyFromDom(parms, unit1);
            foreach (CompilerError oops in results.Errors)
            {
                MessageBox.Show("======== Compiler error ============");
                MessageBox.Show(oops.ErrorText);
            }
            //Invoke the web service method
            object objWS = results.CompiledAssembly.CreateInstance("Service1");
            Type t = objWS.GetType();
            //Console.WriteLine(t.InvokeMember("HelloWorld", System.Reflection.BindingFlags.InvokeMethod, null, objWS, null )); ///i/p object));
            //MessageBox.Show("add  value is   " + t.InvokeMember("GetIotData", System.Reflection.BindingFlags.InvokeMethod, null, objWS, new Object[] { 32, 32 }));

            object  objData = t.InvokeMember("GetIotData", System.Reflection.BindingFlags.InvokeMethod, null, objWS, null);

            iotNode1Data = (IOTNodeData) objData;
            iotNode2Data = (IOTNodeData) objData;

            list.Add(iotNode1Data);
            list.Add(iotNode2Data);
            //Console.WriteLine(iotNode1Data.boolValue);
            //Console.Write(iotNode1Data.strIOTNodeId);

        }
        else
        {
            // Print an error message.
            Console.WriteLine("Warning: " + warning);
        }

    }//end  of  loop

    //-0=-0-=0=-0=-0=-0=-0-=0-

    Console.WriteLine(list[1].boolValue);
    Console.WriteLine(list[1].strIOTNodeId);
    return list;

} //end of getIOTNodeData() function
**/
