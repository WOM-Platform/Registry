/*
 * Totla numero dei voucher generati in un intervallo di tempo.
 *
 * Collection: Vouchers
 */
[
  {
    $match: {
      timestamp: {
        $gte: ISODate("2022-07-01T00:00:00.000Z"),
        $lte: ISODate("2023-07-01T00:00:00.000Z")
      }
    }
  },
  {
    $group: {
      _id: {
        $dateToString: {
          format: "%Y-%m-%d",
          date: "$timestamp"
        }
      },
      totalAmount: {
        $sum: "$count"
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
