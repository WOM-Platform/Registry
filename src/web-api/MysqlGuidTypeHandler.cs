using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api
{
    public class MySqlGuidTypeHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid guid)
            {
                parameter.Value = guid.ToByteArray();
            }

            public override Guid Parse(object value)
            {
                return new Guid((byte[])value);
            }
        }
}
