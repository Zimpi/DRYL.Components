# Specs

One spec per component. `specs/` and `code/` are one artifact — see
[`../harness/requirements.md`](../harness/requirements.md) for the structure,
the `Meta` block and the state rules, and `SPEC-01` for the sync obligation.
The category list is fixed in `SPEC-02`.

The fifteen category folders exist and carry their `_Api.md` and `_Interop.md`
scaffolds; the component specs themselves are reverse-engineered from the
codebase in phase C, category by category. Progress is measured, not estimated:

```
node scripts/check-spec-coverage.mjs
```

It prints `x/127 components covered` and exits non-zero until every component
has exactly one spec claiming it.

Design: [`../docs/2026-08-10-harness-restructure.md`](../docs/2026-08-10-harness-restructure.md).
