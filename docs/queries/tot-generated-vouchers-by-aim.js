/*
 * Totale voucher riscossi ordinati per aim
 *
 * Collection: Vouchers
 */
[
  {
    $lookup: {
      from: "Vouchers",
      localField: "_id",
      foreignField: "generationRequestId",
      as: "vouchersArray"
    }
  },
  {
    $addFields: {
      generatedVoucher: {
        $arrayElemAt: ["$vouchersArray", 0]
      }
    }
  },
  {
    $match: {
      createdAt: {
        $gte: ISODate("2023-07-01T00:00:00.000Z"),
        $lte: ISODate("2024-07-01T00:00:00.000Z")
      }
    }
  },
  {
    $group: {
      _id: {
        $dateToString: {
          format: "%Y-%m-%d",
          date: "$createdAt"
        }
      },
      totalAmountGenerated: {
        $sum: "$amount"
      }
    }
  },
  {
    $sort:
    /**
     * Provide any number of field/order pairs.
     */
      {
        _id: -1
      }
  }
]
