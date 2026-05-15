#!/usr/bin/env python3
"""Send signed test requests to the candidate's webhook endpoint.

This is a known-good HMAC reference. If the candidate's signature
verification rejects requests from this script, their HMAC logic is wrong
(not this script).
"""

import argparse
import hashlib
import hmac
import json
import sys
from urllib import request as urlrequest
from urllib.error import HTTPError, URLError

ENDPOINT = "http://localhost:8080/webhooks/quotes"
SECRET = b"super-secret-key-do-not-commit"

SAMPLE_PAYLOAD = {
    "quoteId": "Q-1042",
    "opportunityId": "OPP-558",
    "status": "submitted",
    "totalAmount": 12450.00,
    "updatedAt": "2026-05-12T14:32:00Z",
}

OLDER_PAYLOAD = {
    "quoteId": "Q-1042",
    "opportunityId": "OPP-558",
    "status": "draft",
    "totalAmount": 9900.00,
    "updatedAt": "2026-05-01T09:00:00Z",
}


def sign(body: bytes) -> str:
    return hmac.new(SECRET, body, hashlib.sha256).hexdigest()


def send(body: bytes, signature: str) -> None:
    req = urlrequest.Request(
        ENDPOINT,
        data=body,
        method="POST",
        headers={
            "Content-Type": "application/json",
            "X-Signature": signature,
        },
    )
    print(f"→ POST {ENDPOINT}")
    print(f"  body: {body.decode(errors='replace')}")
    print(f"  sig:  {signature[:16]}{'…' if len(signature) > 16 else ''}")
    try:
        with urlrequest.urlopen(req) as resp:
            print(f"← {resp.status} {resp.reason}")
            payload = resp.read().decode(errors="replace")
            if payload:
                print(f"  {payload}")
    except HTTPError as e:
        print(f"← {e.code} {e.reason}")
        try:
            print(f"  {e.read().decode(errors='replace')}")
        except Exception:
            pass
    except URLError as e:
        print(f"× Could not reach {ENDPOINT}: {e.reason}")
        print("  Is your server running?")


def send_payload(payload: dict, *, bad_sig: bool = False) -> None:
    body = json.dumps(payload).encode()
    signature = "0" * 64 if bad_sig else sign(body)
    send(body, signature)


def send_malformed() -> None:
    body = b'{"quoteId": "Q-1042", "totalAmount": '  # truncated JSON
    signature = sign(body)
    send(body, signature)


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    g = p.add_mutually_exclusive_group()
    g.add_argument("--bad-sig", action="store_true",
                   help="send with a bad signature (expect 401)")
    g.add_argument("--replay", action="store_true",
                   help="send the same payload twice (second should be a no-op)")
    g.add_argument("--older", action="store_true",
                   help="send an older updatedAt for an existing quote")
    g.add_argument("--malformed", action="store_true",
                   help="send malformed JSON with a valid signature (expect 400)")
    args = p.parse_args()

    if args.malformed:
        send_malformed()
    elif args.older:
        send_payload(OLDER_PAYLOAD)
    elif args.replay:
        send_payload(SAMPLE_PAYLOAD)
        print()
        send_payload(SAMPLE_PAYLOAD)
    else:
        send_payload(SAMPLE_PAYLOAD, bad_sig=args.bad_sig)
    return 0


if __name__ == "__main__":
    sys.exit(main())
