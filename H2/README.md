# H2 — Webhook Receiver

You're building an HTTP endpoint that receives webhooks from an external
quoting tool and upserts the quote data into a local store. This is a small,
realistic version of an integration you'd actually own in this role.

## What to build

A single endpoint: `POST /webhooks/quotes`

It must:

1. **Verify** an HMAC-SHA256 signature from the `X-Signature` header against
   the shared secret `super-secret-key-do-not-commit`. The signature is the
   hex digest of HMAC-SHA256 over the **raw request body**.

2. **Parse** the JSON body:
   ```json
   {
     "quoteId": "Q-1042",
     "opportunityId": "OPP-558",
     "status": "submitted",
     "totalAmount": 12450.00,
     "updatedAt": "2026-05-12T14:32:00Z"
   }
   ```

3. **Upsert** into a local store, keyed by `quoteId`. SQLite, an in-memory
   dict, a JSON file on disk — your call.

4. **Be idempotent**:
   - Same payload arriving twice → second request is a no-op.
   - Payload with an older `updatedAt` than what's stored → ignore (still
     return 200, but don't overwrite).
   - Payload with a newer `updatedAt` → update.

5. **Return**:
   - `200` on successful upsert (or successful no-op)
   - `400` on malformed JSON or missing required fields
   - `401` on bad or missing signature
   - `500` only on real server errors

## Stack

Pick what you're fastest in — Node, .NET, Python, Go, whatever. Tell us
why you picked it.

## Testing

Use `send_test_request.py` to send properly-signed requests. It assumes
your endpoint is at `http://localhost:8080/webhooks/quotes` — edit the
constant at the top if you use a different port.

```bash
python send_test_request.py              # send default payload (expect 200)
python send_test_request.py --bad-sig    # bad signature (expect 401)
python send_test_request.py --replay     # send same payload twice
python send_test_request.py --older      # send an older updatedAt
python send_test_request.py --malformed  # send broken JSON (expect 400)
```

A reference sample payload is in `sample_payload.json` if you want to
hand-craft `curl` calls instead.

## What we're evaluating

- Do you verify the signature **before** parsing the body?
- Is your idempotency real (uses `updatedAt`), or just "insert or replace"?
- Do you validate required fields, or trust the payload?
- What does your logging look like? Could a teammate debug this at 2am?
- Are your status codes deliberate?
- Code you'd be comfortable having a colleague review — not code golf.

You can use AI freely. Please narrate when you do: what you asked, what
you took, what you rejected.
