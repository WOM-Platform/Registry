/*
 * Numero di voucher di uno specifico Aim generati in uno specifico anno, per Instrument.
 *
 * Collection: Vouchers
 */
 
[
  {
    $match:
      /**
       * query: The query in MQL.
       */
      {
        $and: [
          {
            $expr: {
              $eq: [
                {
                  $year: "$timestamp"
                },
                2023
              ]
            }
          },
          {
            aimCode: "C"
          }
        ]
      }
  },
  {
    $lookup:
      /**
       * from: The target collection.
       * localField: The local join field.
       * foreignField: The target join field.
       * as: The name for the results.
       * pipeline: Optional pipeline to run on the foreign collection.
       * let: Optional variables to use in the pipeline field stages.
       */
      {
        from: "GenerationRequests",
        localField: "generationRequestId",
        foreignField: "_id",
        as: "generation"
      }
  },
  {
    $group:
      /**
       * _id: The id of the group.
       * fieldN: The first field name.
       */
      {
        _id: {
          $first: "$generation.sourceId"
        },
        totalCount: {
          $sum: "$initialCount"
        }
      }
  },
  {
    $lookup:
      /**
       * from: The target collection.
       * localField: The local join field.
       * foreignField: The target join field.
       * as: The name for the results.
       * pipeline: Optional pipeline to run on the foreign collection.
       * let: Optional variables to use in the pipeline field stages.
       */
      {
        from: "Sources",
        localField: "_id",
        foreignField: "_id",
        as: "source"
      }
  },
  {
    $project:
      /**
       * specifications: The fields to
       *   include or exclude.
       */
      {
        sourceId: "$_id",
        totalCount: 1,
        sourceName: {
          $first: "$source.name"
        }
      }
  }
]