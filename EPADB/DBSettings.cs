using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPADB {
    public class DBSettings {
        public string Address { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string DBName { get; set; }

        protected static DBSettings single { get; set; }

        public static DBSettings Single { get; set; }

        public static void init() {
            single = XMLSer<DBSettings>.fromXML("Data/DBSettings.xml");
            Single = single;
        }

        public static SqlConnection getConnection() {
            string conStr=String.Format(@"Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};Connection Timeout={4}",
                Single.Address, Single.DBName, Single.User, Single.Password, 3);
            return new SqlConnection(conStr);
        }
        
    }
}
