/*
 * Voucher totali, disponibili (non consumati) e riscattati per Instrument.
 *
 * Collection: Vouchers
 */

[
  {
    /**
     * Associa ogni blocco di voucher con richiesta di generazione.
     *
     * from: The target collection.
     * localField: The local join field.
     * foreignField: The target join field.
     * as: The name for the results.
     * pipeline: The pipeline to run on the joined collection.
     * let: Optional variables to use in the pipeline field stages.
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
     *
     * field: The field name
     * expression: The expression.
     */
    $set: {
      generationRequest: {
        $arrayElemAt: ["$generationRequests", 0]
      }
    }
  },
  {
    /**
     * Raggruppa per sorgente.
     *
     * _id: The id of the group.
     * fieldN: The first field name.
     */
    $group: {
      _id: "$generationRequest.sourceId",
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
    $match:
      /**
       * Opzionale filtro su source ID.
       */
      {
        _id: ObjectId("663db1e7007d0c7329bd8a33")
      }
  }
]