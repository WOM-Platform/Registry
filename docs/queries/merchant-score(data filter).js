/*
 * Posizionamento dei merchant in base ai voucher consumati con il filtro della data
 *
 * Collection: PaymentRequests
 */
[
  {
    $unwind: {
      path: "$confirmations",
      includeArrayIndex: "string",
      preserveNullAndEmptyArrays: false
    }
  },
  {
    $match:
      {
        "confirmations.performedAt": {
          $gte: ISODate(
            "2023-01-01T00:00:00.000Z"
          ),
          $lte: ISODate(
            "2024-01-01T00:00:00.000Z"
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
        $sum: "$amount"
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
