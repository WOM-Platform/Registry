/*
 * Vouchers totali, disponibili (non consumati) e riscossi per Aim.
 *
 * Collection: Vouchers
 */
 
[
  {
    /**
     * Rimuovi voucher senza aim.
     */
    $match: {
      aimCode: {
        $ne: ""
      }
    }
  },
  {
    /**
     * Join su GenerationRequests.
     */
    $lookup: {
      from: "GenerationRequests",
      localField: "generationRequestId",
      foreignField: "_id",
      as: "generationRequests"
    }
  },
  {
    /**
     * Trasforma array in campo singolo.
     */
    $set: {
      generationRequest: {
        $arrayElemAt: ["$generationRequests", 0]
      }
    }
  },
  {
    /**
     * Raggruppamento per Aim, calcolo dei vari totali con metodi di aggregazione.
     */
    $group: {
      _id: "$aimCode",
      totalCount: {
        $sum: "$initialCount"
      },
      availableCount: {
        $sum: {
          $ifNull: ["$count", "$initialCount"]
        }
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
  },
  {
    /**
     * Ordine per codice Aim.
     */
    $sort: {
      _id: 1
    }
  }
]