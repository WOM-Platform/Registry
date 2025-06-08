using System.ComponentModel.DataAnnotations;

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
