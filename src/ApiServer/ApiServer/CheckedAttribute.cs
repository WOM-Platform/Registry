using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public class CheckedAttribute : ValidationAttribute {

        public CheckedAttribute() {
        }

        public override bool IsValid(object value) {
            if((bool)value) {
                return true;
            }

            return false;
        }

    }

}
