using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using SolarWinds.InformationService.Contract2;

namespace SolarWinds.InformationService.InformationServiceClient
{
    /// <summary>
    /// Represents a SWQL statement to execute against a SolarWinds Information Service.
    /// </summary>
    public sealed class InformationServiceCommand : DbCommand
    {
        private string statement;
        private InformationServiceConnection connection;
        private bool designTimeVisible = true;
        private CommandType commandType = CommandType.Text;

        internal InformationServiceCommand(InformationServiceConnection connection)
            : this(string.Empty, connection)
        {

        }

        public InformationServiceCommand(string statement)
        {
            if (statement == null)
                throw new ArgumentNullException(nameof(statement));

            this.statement = statement;
        }

        public InformationServiceCommand(string statement, InformationServiceConnection connection)
            : this(statement)
        {
            this.connection = connection;
        }

        public override void Cancel()
        {
            // Does nothing
        }

        public override string CommandText
        {
            get
            {
                return (statement ?? string.Empty);
            }
            set
            {
                statement = value;
            }
        }

        public override int CommandTimeout { get; set; } = 30;

        public override CommandType CommandType
        {
            get
            {
                return commandType;
            }
            set
            {
                if (value != CommandType.Text)
                    throw new NotSupportedException("InformationServiceCommand only support commands of type Text");
                commandType = value;
            }
        }

        protected override DbParameter CreateDbParameter()
        {
            return new InformationServiceParameter();
        }

        protected override DbConnection DbConnection
        {
            get
            {
                return connection;
            }
            set
            {
                connection = (InformationServiceConnection)value;
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return Parameters; }
        }

        public new InformationServiceParameterCollection Parameters { get; } = new InformationServiceParameterCollection();

        protected override DbTransaction DbTransaction
        {
            get
            {
                return null;
            }
            set
            {
                throw new NotSupportedException("Transactions are not supported");
            }
        }

        public override bool DesignTimeVisible
        {
            get
            {
                return designTimeVisible;
            }
            set
            {
                designTimeVisible = value;
                TypeDescriptor.Refresh(this);
            }
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return ExecuteReader(behavior);
        }

        public override int ExecuteNonQuery()
        {
            throw new NotSupportedException();
        }

        public override object ExecuteScalar()
        {
            throw new NotSupportedException();
        }

        public override void Prepare()
        {
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get
            {
                return UpdateRowSource.None;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public string ApplicationTag { get; set; }

        /// <summary>
        /// Specify expected DateTime.Kind for DateTime values. Default behavior is DataSetDateTime.Unspecified 
        /// and we return raw pure DateTime based on query you used.
        /// </summary>
        public DataSetDateTime DateTimeMode { get; set; } = DataSetDateTime.Unspecified;

        public new InformationServiceDataReader ExecuteReader()
        {
            return ExecuteReader(CommandBehavior.Default);
        }

        public new InformationServiceDataReader ExecuteReader(CommandBehavior behavior)
        {
            //TODO: Check if the statement contains a RETURN clause
            string query = statement + " RETURN XML RAW";

            var bag = new PropertyBag();
            foreach (InformationServiceParameter parameter in Parameters)
                bag.Add(parameter.ParameterName, parameter.Value);

            QueryXmlRequest queryRequest = new QueryXmlRequest(query, bag);

            Message message = null;

            var swisSettingsContext = SwisSettingsContext.Current;
            if (swisSettingsContext != null)
            {
                var originalContextApplicationTag = swisSettingsContext.ApplicationTag;
                swisSettingsContext.AppendErrors = true; // we always want to add errors 

                // only set its value if the ApplicationTag is not set.
                // If it is set in SwisSettingsContext, just use it
                if (string.IsNullOrEmpty(swisSettingsContext.ApplicationTag))
                {
                    swisSettingsContext.ApplicationTag = ApplicationTag;
                }

                try
                {
                    // SwisSettingsContext.ApplicationTag is sent in request to SWIS server
                    message = Query(queryRequest);
                }
                finally
                {
                    // make sure we don't change the value by calling this function
                    swisSettingsContext.ApplicationTag = originalContextApplicationTag;
                }
            }
            else
            {
                using (new SwisSettingsContext
                {
                    DataProviderTimeout = TimeSpan.FromSeconds(CommandTimeout),
                    ApplicationTag = ApplicationTag,
                    AppendErrors = true
                })
                {
                    message = Query(queryRequest);
                }
            }

            if (message != null)
            {
                if (message.IsFault)
                    CreateFaultException(message);

                return new InformationServiceDataReader(this, message.GetReaderAtBodyContents(), DateTimeMode);
            }
            return null;
        }

        private Message Query(QueryXmlRequest queryRequest)
        {
            return connection.Service.Query(queryRequest);
        }

        //TODO: Need to refactor with the code already present in InformationServiceQuery
        private static void CreateFaultException(Message message)
        {
            //TODO: Get the maxFaultSize from somewhere other than a hardcoded version.
            MessageFault messageFault = MessageFault.CreateFault(message, 0x10000);
            XmlDictionaryReader reader = messageFault.GetReaderAtDetailContents();

            DataContractSerializer serializer = new DataContractSerializer(typeof(InfoServiceFaultContract));
            InfoServiceFaultContract faultContract = (InfoServiceFaultContract)serializer.ReadObject(reader);

            throw new FaultException<InfoServiceFaultContract>(faultContract, messageFault.Reason, messageFault.Code, message.Headers.Action);
        }
    }
}
