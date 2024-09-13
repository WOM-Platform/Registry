/*
 * Totale voucher generati e riscossi (filtro per periodo di tempo e per instrument)
 *
 * Collection: Vouchers
 */
[
  {
    $match:
    /**
     * If date is specified
     */
      {
        timestamp: {
          $gte: ISODate("2021-04-03"),
          $lte: ISODate("2024-04-03")
        }
      }
  },
  {
    $lookup: {
      from: "GenerationRequests",
      localField: "generationRequestId",
      foreignField: "_id",
      as: "generationRequest"
    }
  },
  {
    $unwind: {
      path: "$generationRequest",
      includeArrayIndex: "string",
      preserveNullAndEmptyArrays: true
    }
  },
  {
    $lookup:
    /**
     * If instrument name is specified
     */
      {
        from: "Sources",
        localField: "generationRequest.sourceId",
        foreignField: "_id",
        as: "source"
      }
  },
  {
    $unwind:
    /**
     * If instrument name is specified
     */
      {
        path: "$source",
        includeArrayIndex: "string",
        preserveNullAndEmptyArrays: true
      }
  },
  {
    $match:
    /**
     * If instrument name is specified
     */
      {
        "source.name": {
          $eq: "diAry Digital Arianna"
        }
      }
  },
  {
    $project: {
      source: 1,
      initialCount: 1,
      "generationRequest.performedAt": 1
    }
  },
  {
    $group: {
      _id: null,
      generatedCount: {
        $sum: "$initialCount"
      },
      redeemedCount: {
        $sum: {
          $cond: {
            if: "$generationRequest.performedAt",
            then: "$initialCount",
            else: 0
          }
        }
      }
    }
  }
]
