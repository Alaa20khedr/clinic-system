# Clinic System: Test Plan and Test Cases

Covers the full cycle: database, slot generation, booking, patients, doctors, schedules, cancel/complete, security, and UI.

Priority: **P1** = must pass before delivery, **P2** = important, **P3** = nice to have.

## 0. Setup and conventions

- Use a fresh database: `dotnet ef database update -p Clinic.DAL -s Clinic.PL`.
- Seed: clinic 1; Dr. Ahmed (Id 1): Saturday to Thursday 16:00 to 20:00, no Friday; Dr. Sara (Id 2): Sunday, Tuesday, Thursday 10:00 to 14:00.
- Dates: **S** = a future Saturday, **F** = a future Friday, **SUN** and **MON** = future Sunday and Monday, **T** = today. All times are local clinic time.
- Reset between runs:

```sql
DELETE FROM Appointments;
DELETE FROM Patients;
```

- The UI blocks past bookings, so to test Complete, move an appointment into the past:

```sql
UPDATE Appointments
SET StartAt = DATEADD(HOUR,-3,StartAt), EndAt = DATEADD(HOUR,-3,EndAt)
WHERE Id = 1;
```

- Quick slot API check: `/Appointments/GetFreeSlots?doctorId=1&date=yyyy-MM-dd` returns JSON.

## 1. Database and migrations

| ID | Check | Expected | Pri |
|---|---|---|---|
| DB-01 | Run migrations on an empty database | 5 tables created, no errors | P1 |
| DB-02 | Dr. Ahmed schedule rows | 6 rows, DayOfWeek 0,1,2,3,4,6 and no 5 | P1 |
| DB-03 | Dr. Sara schedule rows | 3 rows, DayOfWeek 0,2,4, 10:00 to 14:00 | P1 |
| DB-04 | SQL insert: two Booked appointments, same doctor and StartAt | Second insert fails (unique index) | P1 |
| DB-05 | Same as DB-04 but the first has Status = 2 | Insert succeeds | P1 |
| DB-06 | Insert schedule with EndTime <= StartTime | CHECK constraint error | P2 |
| DB-07 | Insert appointment with EndAt <= StartAt | CHECK constraint error | P2 |
| DB-08 | Delete a doctor, patient or clinic that has children via SQL | FK error (Restrict) | P2 |
| DB-09 | Patients.PhoneNumber and DateOfBirth | NOT NULL, phone is nvarchar(11) | P2 |
| DB-10 | `dotnet ef migrations has-pending-model-changes` | No pending changes | P2 |
| DB-11 | Apply the hardening migration on a DB with patients having NULL phone or DOB | Succeeds, rows backfilled | P3 |

## 2. Slot generation

| ID | Steps | Expected | Pri |
|---|---|---|---|
| SLOT-01 | Ahmed + S | 8 slots, 04:00 PM to 07:30 PM | P1 |
| SLOT-02 | Ahmed + F | No slots, message "No available slots..." | P1 |
| SLOT-03 | Sara + SUN | 8 slots, 10:00 AM to 01:30 PM | P1 |
| SLOT-04 | Sara + MON | No slots (day off) | P1 |
| SLOT-05 | No doctor selected | Dropdown stays empty, no errors | P2 |
| SLOT-06 | Change doctor then date repeatedly | List reloads correctly each time, loading state shown | P2 |
| SLOT-07 | Call GetFreeSlots with yesterday's date | Empty list | P1 |
| SLOT-08 | Today (Ahmed, not Friday) before 16:00 | All 8 slots | P2 |
| SLOT-09 | Today at about 17:10 | Only slots starting after now (17:30 onward) | P1 |
| SLOT-10 | Today after 20:00 | No slots | P2 |
| SLOT-11 | Book 17:00, reload slots for same doctor and date | 17:00 gone, 7 remain | P1 |
| SLOT-12 | Same date, other doctor | Unaffected | P2 |
| SLOT-13 | Cancel the 17:00 booking | 17:00 returns | P1 |
| SLOT-14 | Set the booking Status to 3 (Completed) in SQL | Slot stays blocked | P2 |
| SLOT-15 | Add a second shift 10:00 to 12:00 for Ahmed on S | Slots from both shifts, ordered | P2 |
| SLOT-16 | Back-to-back shifts 10 to 12 and 12 to 14 | Contiguous, no gaps or duplicates | P3 |
| SLOT-17 | Shift ends 20:00 | Last slot is 19:30 to 20:00, no 20:00 slot | P1 |

## 3. Booking with a new patient

| ID | Steps | Expected | Pri |
|---|---|---|---|
| BOOK-01 | New patient, Ahmed, S, 16:00, Save | Success message, row appears in the grid | P1 |
| BOOK-02 | Check the DB | StartAt = S 16:00, EndAt = 16:30, Status = 1; one new Patients row with ClinicId = 1 | P1 |
| BOOK-03 | Refresh (F5) after success | No duplicate booking (Post-Redirect-Get) | P1 |
| BOOK-04 | Check grid columns | Patient, doctor, date, start, end (30 min), status Booked | P1 |
| BOOK-05 | Book the same patient data again at 16:30 | Second appointment, Patients still 1 row | P1 |
| BOOK-06 | Same phone, different name | New patient row (by design) | P3 |
| BOOK-07 | Phone entered as +201012345678 | Stored as 01012345678 | P1 |
| BOOK-08 | Book while the grid is filtered by date and doctor | Filter kept after save | P2 |
| BOOK-09 | Trigger a server error (slot taken) | Form re-renders with typed values preserved, slots reloaded | P1 |
| BOOK-10 | Check durations of all bookings | Always 30 minutes | P1 |
| BOOK-11 | Book the last slot of the day | Works, ends 20:00 | P2 |
| BOOK-12 | Book today at a future slot | Works | P2 |

## 4. Booking with an existing patient

| ID | Steps | Expected | Pri |
|---|---|---|---|
| EXIST-01 | Create a patient in the Patients page, then type the first 2 letters in the booking search | Patient appears in results | P1 |
| EXIST-02 | Select the patient | Fields filled and locked, banner shown | P1 |
| EXIST-03 | Save the booking | Appointment saved, no new patient row | P1 |
| EXIST-04 | Search by 01012345678 and by +201012345678 | Same patient found | P1 |
| EXIST-05 | Type 1 character | No results, no request | P3 |
| EXIST-06 | Search with no match | "No matching patient" message | P2 |
| EXIST-07 | Click Clear | Fields unlocked and emptied, PatientId cleared | P1 |
| EXIST-08 | Clear, type a new patient manually, Save | New patient row created | P1 |
| EXIST-09 | Select a patient who already has past appointments | Booking works | P2 |
| EXIST-10 | Edit the locked fields via dev tools, Save | Server uses PatientId, stored patient data unchanged | P1 |
| EXIST-11 | POST a PatientId that does not exist | Friendly error, no 500 | P2 |
| EXIST-12 | Patient name containing a script tag (inserted via SQL) | Shown as plain text in the results, nothing executes | P1 |
| EXIST-13 | More than 10 matches | Maximum 10 shown | P3 |

## 5. Validation (client, server and database)

### 5.1 Patient name

| ID | Input | Expected | Pri |
|---|---|---|---|
| VAL-N01 | Empty | Required error | P1 |
| VAL-N02 | `A` (1 character) | Length error | P2 |
| VAL-N03 | 151 characters | Length error | P2 |
| VAL-N04 | `12345` | Rejected | P1 |
| VAL-N05 | `Ahmed123` | Rejected | P1 |
| VAL-N06 | A script tag | Rejected, nothing saved | P1 |
| VAL-N07 | Arabic name, for example `محمد علي` | Accepted | P1 |
| VAL-N08 | `O'Brien`, `Abdel-Rahman`, `Dr. Ali` | Accepted | P2 |
| VAL-N09 | Extra spaces around and between words | Saved with single spaces, trimmed | P2 |
| VAL-N10 | Spaces only | Required error | P2 |

### 5.2 Date of birth

| ID | Input | Expected | Pri |
|---|---|---|---|
| VAL-D01 | Empty | Required error | P1 |
| VAL-D02 | Tomorrow | Blocked by the picker and rejected by the server | P1 |
| VAL-D03 | Today | Accepted | P3 |
| VAL-D04 | Exactly 120 years ago, then 121 years ago | 120 accepted, 121 rejected | P2 |
| VAL-D05 | Future DOB forced through dev tools | Server rejects | P1 |

### 5.3 Phone number (Egyptian mobile only)

| ID | Input | Expected | Pri |
|---|---|---|---|
| VAL-P01 | 01012345678, 01112345678, 01212345678, 01512345678 | Accepted | P1 |
| VAL-P02 | +201012345678 | Accepted, saved as 01012345678 | P1 |
| VAL-P03 | 00201012345678 | Accepted, saved as 01012345678 | P1 |
| VAL-P04 | 010-1234-5678 and 010 1234 5678 | Accepted, saved as 01012345678 | P1 |
| VAL-P05 | 0123 | Rejected | P1 |
| VAL-P06 | 02-12345678 (landline) | Rejected | P1 |
| VAL-P07 | 01312345678 (invalid prefix) | Rejected | P1 |
| VAL-P08 | 010123456789 (12 digits) and 0101234567 (10 digits) | Rejected | P1 |
| VAL-P09 | 01012abc678 | Rejected | P1 |
| VAL-P10 | Empty or spaces only | Required error | P1 |
| VAL-P11 | Any rejected value | Message: "Enter a valid Egyptian mobile number (e.g. 01012345678)." Same message on client and server | P2 |

### 5.4 Appointment fields

| ID | Steps | Expected | Pri |
|---|---|---|---|
| VAL-A01 | No doctor selected | Error | P1 |
| VAL-A02 | Empty date | Error | P1 |
| VAL-A03 | Remove the date min attribute and submit a past date | Server rejects | P1 |
| VAL-A04 | No slot selected | Error | P1 |
| VAL-A05 | Change the slot option value to 03:00 and submit | "No longer available" error | P1 |
| VAL-A06 | Submit doctorId = 999 | "Doctor not found" | P2 |
| VAL-A07 | Disable JavaScript or bypass client validation | Server rejects every invalid input | P1 |
| VAL-A08 | Submit without the anti-forgery token | 400 | P1 |

## 6. Concurrency

| ID | Steps | Expected | Pri |
|---|---|---|---|
| CONC-01 | Two tabs, same doctor, date and slot. Save in tab 1, then tab 2 | Tab 2 shows "slot was just booked" or "no longer available"; only 1 appointment in DB | P1 |
| CONC-02 | Two tabs, different slots | Both succeed | P2 |
| CONC-03 | Same slot, different doctors | Both succeed | P2 |
| CONC-04 | Double-click Save quickly | Exactly 1 appointment and at most 1 new patient (failed save is rolled back) | P1 |
| CONC-05 | After the CONC-01 failure, pick another slot and resubmit | Works, typed values preserved | P2 |
| CONC-06 | Cancel in tab A while tab B books the same slot | No duplicates; B succeeds once the cancel is done | P2 |
| CONC-07 | Known limitation: two simultaneous bookings for the same brand-new patient | May create duplicate patient rows (no unique index on Patients); documented in the README | P3 |

## 7. Cancel and Complete

| ID | Steps | Expected | Pri |
|---|---|---|---|
| ACT-01 | Cancel a Booked appointment, confirm | Status Cancelled (grey), buttons disappear, success message | P1 |
| ACT-02 | Open the cancel dialog and dismiss it | Nothing changes | P2 |
| ACT-03 | After cancel, check slots and rebook the same slot | Slot is available and booking succeeds | P1 |
| ACT-04 | Cancel while the grid is filtered | Filters preserved | P2 |
| ACT-05 | Replay the cancel POST on a Cancelled appointment | Error message, no 500 | P2 |
| ACT-06 | Cancel a Completed appointment | Error message | P2 |
| ACT-07 | Complete a future appointment | Rejected: "has not started yet" | P1 |
| ACT-08 | Move the appointment into the past via SQL, then Complete | Status Completed (blue) | P1 |
| ACT-09 | Complete a Cancelled appointment | Error message | P2 |
| ACT-10 | Cancel or Complete an unknown id | "Not found" message, no 500 | P2 |
| ACT-11 | Open `/Appointments/Cancel/1` with GET | 404 or 405, nothing changes | P1 |
| ACT-12 | Check a Completed row | No action buttons | P2 |

## 8. Grid and filters

| ID | Steps | Expected | Pri |
|---|---|---|---|
| GRID-01 | Empty database | Empty state message | P2 |
| GRID-02 | Several bookings created out of order | Sorted by start time | P2 |
| GRID-03 | Filter by date | Only that day | P1 |
| GRID-04 | Filter by doctor | Only that doctor | P1 |
| GRID-05 | Filter by date and doctor | Both applied | P1 |
| GRID-06 | Reset | All appointments shown, filter cleared | P2 |
| GRID-07 | Filter on a date with none | Empty state | P2 |
| GRID-08 | Check formats | Times like 05:00 PM, dates consistent, status shown as text not numbers | P2 |
| GRID-09 | Open appointment Details | Correct patient, doctor and times | P2 |
| GRID-10 | Details with unknown id | Friendly 404 | P2 |
| GRID-11 | 50 or more appointments | Page loads quickly | P3 |

## 9. Patients CRUD

| ID | Steps | Expected | Pri |
|---|---|---|---|
| PAT-01 | Open the list | Appointment counts correct | P2 |
| PAT-02 | Search by partial name | Matches shown | P1 |
| PAT-03 | Search by phone in 01..., +20... and 0020... formats | Same patient found | P1 |
| PAT-04 | Create a valid patient | Saved, phone normalized | P1 |
| PAT-05 | Create with the same name, DOB and phone as an existing patient | Duplicate error | P1 |
| PAT-06 | Create with invalid name, DOB or phone | Same errors as section 5 | P1 |
| PAT-07 | Edit and save | Changes persist | P1 |
| PAT-08 | Edit so it matches another patient | Duplicate error | P2 |
| PAT-09 | Edit unknown id | 404 | P2 |
| PAT-10 | POST edit with a mismatched id | 400 | P3 |
| PAT-11 | Open Details | History shown, newest first | P2 |
| PAT-12 | Delete a patient with no appointments | Deleted | P1 |
| PAT-13 | Delete a patient with a Booked appointment | Blocked: "Cannot delete: patient has appointments." | P1 |
| PAT-14 | Delete a patient with only Cancelled appointments | Still blocked (by design, history is kept) | P2 |
| PAT-15 | Delete unknown id | "Not found" message | P2 |
| PAT-16 | Open the Delete URL with GET | Not allowed, nothing deleted | P1 |
| PAT-17 | Edit the phone of a patient with appointments | Grid and Details show the new phone | P2 |

## 10. Doctors CRUD

| ID | Steps | Expected | Pri |
|---|---|---|---|
| DOC-01 | Open the list | Schedule and appointment counts correct | P2 |
| DOC-02 | Create a valid doctor | Saved with ClinicId = 1 | P1 |
| DOC-03 | Name empty, 1 character, 151 characters | Validation errors | P1 |
| DOC-04 | Specialty of 101 characters | Rejected | P2 |
| DOC-05 | Specialty empty | Accepted | P2 |
| DOC-06 | New doctor in the booking dropdown | Listed, but no slots until a schedule is added | P1 |
| DOC-07 | Edit a doctor | Changes persist | P1 |
| DOC-08 | Delete a doctor with schedules but no appointments | Doctor and schedule rows are deleted | P1 |
| DOC-09 | Delete a doctor with appointments | Blocked: "Cannot delete: doctor has appointments." | P1 |
| DOC-10 | Delete a doctor with only Cancelled appointments | Still blocked | P2 |
| DOC-11 | Open Details for Dr. Ahmed | Week listed Saturday to Friday; Friday shows "Day off" | P1 |
| DOC-12 | Details or Edit with unknown id | 404 | P2 |

## 11. Doctor schedules

| ID | Steps | Expected | Pri |
|---|---|---|---|
| SCH-01 | Add Sara Monday 10:00 to 14:00 | Next Monday now has 8 slots | P1 |
| SCH-02 | Add Ahmed Friday 16:00 to 20:00, then delete it | Friday has slots, then none again | P1 |
| SCH-03 | End before start | Rejected | P1 |
| SCH-04 | End equal to start | Rejected | P1 |
| SCH-05 | Off-grid time such as 09:15 | Rejected (30-minute grid) | P1 |
| SCH-06 | Overlap: 16:00 to 20:00 and 18:00 to 21:00 | Rejected | P1 |
| SCH-07 | Same doctor, day and start twice | Rejected | P1 |
| SCH-08 | Adjacent periods 10 to 12 and 12 to 14 | Both allowed | P2 |
| SCH-09 | Two periods in one day | Listed separately in Details | P2 |
| SCH-10 | Extend a period that has a future appointment inside | Allowed | P1 |
| SCH-11 | Shrink a period so a future appointment falls outside | Rejected with a clear message | P1 |
| SCH-12 | Shrink a period that affects no appointment | Allowed | P2 |
| SCH-13 | Move a period to another day while a future appointment is inside | Rejected | P1 |
| SCH-14 | Edit a period so it overlaps another one | Rejected | P2 |
| SCH-15 | Tamper the doctorId in the edit form | Checks still use the real doctor of the row | P2 |
| SCH-16 | Delete a period with a future Booked appointment | Rejected | P1 |
| SCH-17 | Delete a period with only past or Cancelled appointments | Allowed | P2 |
| SCH-18 | After deleting the only period of a day | Day shows "Day off", no slots | P1 |
| SCH-19 | Delete unknown schedule id | "Not found" message | P3 |

## 12. Security and robustness

| ID | Steps | Expected | Pri |
|---|---|---|---|
| SEC-01 | View source of every POST form | Anti-forgery token present | P1 |
| SEC-02 | POST without a token | 400 | P1 |
| SEC-03 | Use XSS strings in names, specialty and search | Always encoded: grid, lists, details, dropdown | P1 |
| SEC-04 | Search with `' OR 1=1 --` | No error, no extra data | P1 |
| SEC-05 | Very long input (10,000 characters) | Validation error, no crash | P3 |
| SEC-06 | Unknown URL, and a forced exception outside Development | Friendly 404 or error page, no stack trace | P2 |
| SEC-07 | Ids 0 and -1 in all routes | Not found, no 500 | P2 |
| SEC-08 | `filterDate=abc` in the query string | No 500 | P2 |
| SEC-09 | Open the HTTP URL | Redirects to HTTPS | P3 |
| SEC-10 | Open `/Clinics` | 404, no Clinic UI anywhere | P3 |

## 13. UI and UX

| ID | Check | Expected | Pri |
|---|---|---|---|
| UI-01 | Navbar | Active link highlighted | P2 |
| UI-02 | Widths 375, 768 and 1280 px | No horizontal page scroll (tables scroll inside their wrapper) | P2 |
| UI-03 | Success and error feedback | Visible message after every action | P2 |
| UI-04 | Slot loading and empty states | Loading indicator, then slots or a clear message | P2 |
| UI-05 | Cancel, Complete and Delete | Confirmation shown first | P2 |
| UI-06 | Invalid fields | Message under each field, invalid style | P2 |
| UI-07 | Keyboard only | Logical tab order, Enter submits, Esc closes modals | P3 |
| UI-08 | Browser console on every page | No errors | P2 |
| UI-09 | Chrome, Edge and Firefox | Same behavior | P3 |
| UI-10 | Date pickers | min and max attributes work | P2 |

## 14. End-to-end scenarios

| ID | Scenario | Expected | Pri |
|---|---|---|---|
| E2E-01 | Create Dr. Mona, add Saturday 09:00 to 12:00, create a patient, book 09:30 for next Saturday, move it to the past via SQL, Complete it | Every step succeeds; Details shows the Completed visit | P1 |
| E2E-02 | Book 17:00, cancel it, have another patient book the same slot | Two rows: one Cancelled and one Booked | P1 |
| E2E-03 | Book Sara on Sunday 11:00, try to delete her Sunday shift, cancel the booking, delete the shift, add it again | Blocked first, allowed after the cancel, Sunday returns after re-adding | P1 |
| E2E-04 | New patient and doctor with no appointments are deleted; ones with a Booked appointment are blocked; cancelling does not unblock | Matches the delete rules above | P1 |
| E2E-05 | Create a patient in Patients, book via search, then book a brand-new manual patient, then retype the same manual data | Patient count: unchanged, plus 1, then unchanged | P1 |
| E2E-06 | Book all 8 slots for Ahmed on S with different patients, then cancel one | Dropdown empties with a message, then exactly one slot returns | P2 |

## 15. Exit criteria and bug report

- All P1 cases pass.
- At least 90 percent of P2 cases pass.
- No test produces an unhandled exception (HTTP 500).
- The known limitation CONC-07 is written in the README.

Bug report template: ID, steps to reproduce, expected result, actual result, screenshot or error text, browser and date.
