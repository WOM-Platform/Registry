using System;
using System.Data;

namespace WomPlatform.Web.Api {

    public class MySqlGuidTypeHandler : Dapper.SqlMapper.TypeHandler<Guid> {
        public override void SetValue(IDbDataParameter parameter, Guid guid) {
            parameter.Value = guid.ToByteArray();
        }

        public override Guid Parse(object value) {
            return new Guid((byte[])value);
        }
    }

}
