namespace WomPlatform.Web.Api.OutputModels.Map {
    public class PosBoxResponse {

        public class PosEntry {
            public string Id { get; init; }

            public string Name { get; init; }

            public GeoCoordsOutput Position { get; init; }

            public string Url { get; init; }
        }

        public PosEntry[] Pos { get; init; }

        public GeoCoordsOutput LowerLeft { get; init; }

        public GeoCoordsOutput UpperRight { get; init; }

    }
}
