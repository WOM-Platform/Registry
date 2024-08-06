/*
 * Instrument con numero di voucher generati in uno specifico anno e lista gestori.
 *
 * Collection: GenerationRequests
 */

[
  {
    $match:
      /**
       * query: The query in MQL.
       */
      {
        isVerified: true,
        performedAt: {
          $exists: true
        },
        $expr: {
          $eq: [
            {
              $year: "$createdAt"
            },
            2023
          ]
        }
      }
  },
  {
    $group:
      /**
       * _id: The id of the group.
       * fieldN: The first field name.
       */
      {
        _id: "$sourceId",
        totalVouchers: {
          $sum: "$totalVoucherCount"
        }
      }
  },
  {
    $sort:
      /**
       * Provide any number of field/order pairs.
       */
      {
        totalVouchers: -1
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
        totalVouchers: 1,
        sourceName: {
          $getField: {
            field: "name",
            input: {
              $arrayElemAt: ["$source", 0]
            }
          }
        },
        sourceAdmins: {
          $getField: {
            field: "adminUserIds",
            input: {
              $arrayElemAt: ["$source", 0]
            }
          }
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
        from: "Users",
        localField: "sourceAdmins",
        foreignField: "_id",
        as: "users"
      }
  },
  {
    $project:
      /**
       * specifications: The fields to
       *   include or exclude.
       */
      {
        _id: 1,
        totalVouchers: 1,
        sourceName: 1,
        userEmails: {
          $reduce: {
            input: "$users.email",
            initialValue: "",
            in: {
              $concat: [
                "$$value",
                {
                  $cond: [
                    {
                      $eq: ["$$value", ""]
                    },
                    "",
                    ", "
                  ]
                },
                "$$this"
              ]
            }
          }
        }
      }
  }
]