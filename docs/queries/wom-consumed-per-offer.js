/*
 * Totale numero dei voucher consumati in base all'offerta per singolo merchant.
 *
 * Collection: Offers
 */

[
  {
    $match: {
      "merchant._id": ObjectId(
        "5fb24fe93922fa0001766b3c"
      )
    }
  },
  {
    $lookup: {
      from: "PaymentRequests",
      localField: "paymentRequestId",
      foreignField: "_id",
      as: "payments"
    }
  },
  {
    $match: {
      payments: {
        $ne: []
      }
    }
  },
  {
    $addFields: {
      totalAmount: {
        $multiply: [
          {
            $size: "$payments"
          },
          "$cost"
        ]
      }
    }
  },
  {
    $sort:
      {
        totalAmount: -1
      }
  },
  {
    $project: {
      offerId: 1,
      title: 1,
      description: 1,
      filter: 1,
      cost: 1,
      totalAmount: 1
    }
  }
]
