/*
 * Posizionamento dei merchant in base ai voucher consumati
 *
 * Collection: PaymentRequests
 */
[
  {
    $match:
    /**
     * query: The query in MQL.
     */
      {
        createdAt: {
          $gte: ISODate(
            "2022-07-01T00:00:00.000Z"
          ),
          $lte: ISODate(
            "2023-07-01T00:00:00.000Z"
          )
        }
      }
  },
  {
    $lookup: {
      from: "Pos",
      localField: "posId",
      foreignField: "_id",
      as: "posData"
    }
  },
  {
    $addFields: {
      merchantId: {
        $arrayElemAt: ["$posData.merchantId", 0]
      }
    }
  },
  {
    $lookup: {
      from: "Merchants",
      localField: "merchantId",
      foreignField: "_id",
      as: "merchant"
    }
  },
  {
    $group: {
      _id: "$merchantId",
      totalAmount: {
        $sum: {
          $multiply: [
            "$amount",
            {
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
          ]
        }
      },
      name: {
        $first: "$merchant.name"
      }
    }
  },
  {
    $project: {
      name: {
        $arrayElemAt: ["$name", 0]
      },
      totalAmount: 1
    }
  },
  {
    $setWindowFields: {
      sortBy: {
        totalAmount: -1
      },
      output: {
        rank: {
          $rank: {}
        }
      }
    }
  },
  {
    $match:
    /**
     * query: The query in MQL.
     */
      {
        _id: ObjectId("62718887cb383e315bd376ff")
      }
  }
]
