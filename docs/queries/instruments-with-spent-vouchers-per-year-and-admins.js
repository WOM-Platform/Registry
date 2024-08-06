/*
 * Merchant con numero di voucher consumati in uno specifico anno e lista gestori.
 *
 * Collection: PaymentRequests
 */

[
  {
    $project:
      /**
       * specifications: The fields to
       *   include or exclude.
       */
      {
        amount: 1,
        filter: 1,
        createdAt: 1,
        posId: 1,
        confirmations: {
          $filter: {
            input: "$confirmations",
            cond: {
              $eq: [
                {
                  $year: "$$this.performedAt"
                },
                2023
              ]
            }
          }
        }
      }
  },
  {
    $addFields:
      /**
       * specifications: The fields to
       *   include or exclude.
       */
      {
        transactionCount: {
          $cond: {
            if: {
              $isArray: "$confirmations"
            },
            then: {
              $size: "$confirmations"
            },
            else: 0
          }
        }
      }
  },
  {
    $addFields: {
      partialTotalAmount: {
        $multiply: [
          "$transactionCount",
          "$amount"
        ]
      }
    }
  },
  {
    $match:
      /**
       * query: The query in MQL.
       */
      {
        transactionCount: {
          $gt: 0
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
        _id: "$posId",
        totaltransactionCount: {
          $sum: "$transactionCount"
        },
        totalAmount: {
          $sum: "$partialTotalAmount"
        }
      }
  },
  // {
  //   $sort:
  //     /**
  //      * Provide any number of field/order pairs.
  //      */
  //     {
  //       totalTransactionCount: -1
  //     }
  // }
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
        from: "Pos",
        localField: "_id",
        foreignField: "_id",
        as: "pos"
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
        from: "Merchants",
        localField: "pos.0.merchantId",
        foreignField: "_id",
        as: "merchant"
      }
  },
  {
    $addFields: {
      posName: {
        $getField: {
          field: "name",
          input: {
            $arrayElemAt: ["$pos", 0]
          }
        }
      },
      merchantId: {
        $getField: {
          field: "_id",
          input: {
            $arrayElemAt: ["$merchant", 0]
          }
        }
      },
      merchantName: {
        $getField: {
          field: "name",
          input: {
            $arrayElemAt: ["$merchant", 0]
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
        localField: "merchant.access.userId",
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
        totalTransactionCount: 1,
        totalAmount: 1,
        posName: 1,
        merchantId: 1,
        merchantName: 1,
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