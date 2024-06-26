﻿using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.Source {
    public class SourceAccessOutput {
        public class UserAccessInformation {
            public ObjectId UserId { get; set; }

            public string Email { get; set; }

            public string Name { get; set; }

            public string Surname { get; set; }

            public SourceRole Role { get; set; }
        }

        public ObjectId SourceId { get; set; }

        public UserAccessInformation[] Users { get; set; }
    }
}
