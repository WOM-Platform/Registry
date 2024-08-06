/*
 * Elenco di Merchant senza POS.
 *
 * Collection: Merchants
 */
 
[
  {
    /**
     * from: The target collection.
     * localField: The local join field.
     * foreignField: The target join field.
     * as: The name for the results.
     * pipeline: The pipeline to run on the joined collection.
     * let: Optional variables to use in the pipeline field stages.
     */
    $lookup: {
      from: "Pos",
      localField: "_id",
      foreignField: "merchantId",
      as: "pos"
    }
  },
  {
    /**
     * specifications: The fields to
     *   include or exclude.
     */
    $project: {
      _id: 1,
      name: 1,
      posCount: {
        $size: "$pos"
      }
    }
  },
  {
    /**
     * query: The query in MQL.
     */
    $match: {
      posCount: 0
    }
  }
]