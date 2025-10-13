/*
 * Dettagli delle offerte, con data dell'ultima conferma di pagamento per ognuna.
 *
 * Collection: Offers
 */
[
  {
    $lookup:
      /**
       * Carica la PaymentRequest relativa.
       */
      {
        from: "PaymentRequests",
        localField: "payment.otc",
        foreignField: "_id",
        as: "paymentRequests",
      },
  },
  {
    $addFields:
      /**
       * Trasforma array $paymentRequests in campo singolo.
       */
      {
        paymentRequest: {
          $arrayElemAt: ["$paymentRequests", 0],
        },
      },
  },
  {
    $project:
      /**
       * Proiezione dell'output.
       */
      {
        _id: 1,
        title: 1,
        deactivated: 1,
        createdOn: 1,
        lastUpdate: 1,
        description: 1,
        payment: 1,
        pos: 1,
        merchant: 1,
		/* Estriamo l'ultima data di conferma pagamento */
        latestPaymentConfirmation: {
          $arrayElemAt: [
            "$paymentRequest.confirmations.performedAt",
            -1,
          ],
        },
      },
  },
]
