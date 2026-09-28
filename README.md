# Claude Code Day 2 · Indus Motor — spec-driven development

The complete delivery package for Day 2 of the Indus Motor Claude Code training (10Pearls).
Same layout as the Day 1 package. **Start with [00-START-HERE.md](00-START-HERE.md).**

| Folder | What it is |
|---|---|
| [01-Instructor/](01-Instructor/) | Checklist, runbook, answer keys, troubleshooting, lab solutions, a reference build |
| [02-Slides/](02-Slides/) | The 48-slide deck (PDF + PPTX with speaker notes) and its HTML source |
| [03-Send-Before-Workshop/](03-Send-Before-Workshop/) | The pre-work email |
| [04-Hand-To-Students/](04-Hand-To-Students/) | Lab guide, quick reference, quizzes, exam, stretch cards, feedback |
| [05-Lab-Repo/](05-Lab-Repo/) | The student repository (`AssetDesk-Lab/`) and the script that publishes it |
| [06-Follow-Up/](06-Follow-Up/) | The 30-day adoption check |

The student repository is **not** this repository. Attendees clone the training repo you publish
from `05-Lab-Repo/` (see [05-Lab-Repo/README.md](05-Lab-Repo/README.md)), so answer keys and the
reference build never reach them.

## Editing the slides

The deck is HTML rendered to PDF by headless Chrome, then packed into a PPTX with speaker notes.

```bash
cd 02-Slides/source
python3 -m pip install -r requirements.txt
python3 build.py
```

Edit `deck.html` (content and speaker notes live side by side), `deck.css` (the 10Pearls design
system taken from the Day 1 deck), or `config.json` (the three form links behind the QR codes).
