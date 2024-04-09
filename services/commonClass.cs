using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace subscription_api.services
{
    public class commonClass
    {

        public class m_specializations
        {
            public int _spcialisation_id { get; set; }
            public string _Specialization_name { get; set; }
        }

        public class m_doctors
        {
            public int _doctor_id { get; set; }
            public string _doctor_name { get; set; }
            public int _spcialisation_id { get; set; }


        }

        public class m_doc_schedules
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string? _id { set; get; }
            public int _doctor_id { get; set; }
            public int _schedule_id { get; set; }
            public string _day_id { get; set; }
            public string _time_from { get; set; }
            public string _time_till { get; set; }
            public string _description { get; set; }
            public string _per_day_pats { get; set; }
            public string _slot_type { get; set; }
        }

        public class m_doctor
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string? _id { set; get; }
            public int _doctor_id { get; set; }
            public string _doctor_name { get; set; }
            public int _schedule_id { get; set; }
            public IEnumerable<m_doc_schedules> inventory_docs { set; get; }


        }
        public class doctor
        {
            public int _doctor_id { get; set; }
            public int _facilty_id { get; set; }


        }
        public class m_facility
        {
            public int _facility_id { get; set; }
            public int _doctor_id { get; set; }
            public string _facility_name { get; set; }
            // public string _facility_mobile_no { get; set; }
            // public string _facilty_email_id { get; set; }
            // public string _facility_type_id { get; set; }
            //  public string _facility_pin_code { get; set; }
            // public string _facility_line_1 { get; set; }
            // public string _facility_line_2 { get; set; }
            //  public string _faclity_coordinates { get; set; }
            //  public string _facilty_estd_date { get; set; }
            // public string _registration_datetime { get; set; }
            // public string _last_update_datetime { get; set; }



        }

        public class degree
        {
            public int _degree_id { get; set; }
            public int _doctor_id { get; set; }
            public string _qua_degree_details { get; set; }
        }
        public class m_certs
        {
            public int _doctor_cert_id { get; set; }
            public int _doctor_id { get; set; }
            public string _cert_details { get; set; }
        }
        
    }
}