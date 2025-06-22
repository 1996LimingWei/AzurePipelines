import pyodbc
import requests
from datetime import datetime
import sys

password = sys.argv[1]
env_type = sys.argv[2]
LogicAppUrl = sys.argv[3]
Mismatchtable = sys.argv[4]
Reporttable = sys.argv[5]
JobTime = sys.argv[6]
print('JobTime is: ', JobTime)

def ReadDB_mismatchOutput():
    return execute_sql_command("""
        SELECT [StartTime],
               [EndTime],
               [Srclanguage],
               [Tgtlanguage],
               [TestSet],
               [NegativeFlag],
               [TotalCount] AS TotalRequest,
               [ValidOutputCount] AS TotalFailures,
               [ScoreType],
               [RunID],
               [Url]
        FROM {};
    """.format(Mismatchtable))

def ReadDB_mailReport():
    return execute_sql_command("""
        SELECT [RunID],
               [StartTime],
               [Environment],
               [Srclanguage],
               [Tgtlanguage],
               [ScoreType],
               [TestSet],
               [NegativeFlag],
               [ScoreValue],
               [BaselineRunId],
               [BaselineStartTime],
               [BaselineScoreValue],
               [Delta],
               [EndTime],
               [Url],
               [BaselineEndTime]
        FROM {};
    """.format(Reporttable))

def execute_sql_command(sql_command):
    conn = pyodbc.connect(
        "Driver={ODBC Driver 17 for SQL Server};"
        "Server=gender-debias-server.database.windows.net;"
        "Database=GenderDebiasDatabase;"
        "UID=CloudSAc05a6b11;"
        f"PWD={password};"
    )
    cursor = conn.cursor()
    cursor.execute(sql_command)
    rows = cursor.fetchall()
    cursor.close()
    conn.close()
    return rows

def GenerateEmailBody():
    mismatch_rows = ReadDB_mismatchOutput()
    mismatched_rows = [row for row in mismatch_rows if row[6] != row[7]]
    mail_report_rows = ReadDB_mailReport()
    merged_mail_report_rows = merge_rows(mail_report_rows)

    mismatch_table_header = "<tr><th>StartTime</th><th>EndTime</th><th>Srclanguage</th><th>Tgtlanguage</th><th>TestSet</th><th>NegativeFlag</th><th>TotalRequest</th><th>TotalFailures</th><th>ScoreType</th><th>RunID</th><th>Url</th></tr>"
    mail_report_table_header = "<tr><th>Srclanguage</th><th>Tgtlanguage</th><th>ScoreType</th><th>TestSet</th><th>NegativeFlag</th><th>ScoreValue</th><th>BaselineScoreValue</th><th>Delta</th><th>Url</th></tr>"
    mail_report_table_rows = generate_table_rows(merged_mail_report_rows, mail_report_table_header)
    mismatched_rows = [[row[0], row[1], row[2], row[3], row[4], row[5], row[6], row[6] - row[7], row[8], row[9], row[10]] for row in mismatched_rows]

    mismatch_table_rows = generate_table_rows(mismatched_rows, mismatch_table_header)

    StartTime_time = mail_report_rows[0][1] if mail_report_rows else None    # Get run start time
    EndTime_time = mail_report_rows[0][13] if mail_report_rows else None    # Get run end time
    baseline_start_time = mail_report_rows[0][10] if mail_report_rows else None  # Get baseline start time
    baseline_end_time = mail_report_rows[0][15] if mail_report_rows else None  # Get baseline end time
    baseline_run_id = mail_report_rows[0][9] if mail_report_rows else None  # Get baseline run ID
    email_body = f"<p>Environment: <strong>{env_type}</strong>"
    email_body += f"<p>Job StartTime: <strong>{StartTime_time}</strong>"
    email_body += f"<p>Job EndTime: <strong>{EndTime_time}</strong>"
    email_body += f"<p>RunID: <strong>{mail_report_rows[0][0]}</strong>"
    email_body += f"<p>Baseline Job StartTime: <strong>{baseline_start_time}</strong>"
    email_body += f"<p>Baseline Job StartTime: <strong>{baseline_end_time}</strong>"
    email_body += f"<p>BaselineRunID: <strong>{baseline_run_id}</strong>"
    email_body += "</p>"
    email_body += f"<table border='1' cellpadding='5'>{mail_report_table_header}{mail_report_table_rows}</table>"

    if mismatched_rows:
        email_body += f"<p>Please investigate the error.txt in https://genderdebiasstorage.blob.core.windows.net/genderdebias/PerfEval/{JobTime}/Error.txt. The valid output count does not match the expected output count. :</p>"
        email_body += f"<table border='1' cellpadding='5'>{mismatch_table_header}{mismatch_table_rows}</table>"

    email_body += "</body></html>"

    return email_body, mismatched_rows

def generate_table_rows(rows, table_header):
    table_rows = "".join(
        f"<tr>{''.join(f'<td>{value}</td>' for value in row)}</tr>" for row in rows
    )
    return table_rows

def merge_rows(rows):
    merged_rows = []
    merged_dict = {}

    for row in rows:
        key = (row[3], row[4], row[6], row[9], row[7])  # SourceLang, TargetLang, TestSet, BaselineRunId, NegativeFlag
        if key not in merged_dict:
            merged_dict[key] = {
                "SourceLang": row[3],
                "TargetLang": row[4],
                "TestSet": row[6],
                "NegativeFlag": row[7],
                "ScoreType": [],
                "ScoreValue": [],
                "BaselineScoreValue": [],
                "Delta": [],
                "Url": [],
            }
        merged_dict[key]["ScoreType"].append(row[5])
        merged_dict[key]["ScoreValue"].append(row[8])
        merged_dict[key]["BaselineScoreValue"].append(row[11])
        merged_dict[key]["Delta"].append(row[12])
        if not merged_dict[key]["Url"]:
            merged_dict[key]["Url"].append(row[14])

    for key, values in merged_dict.items():
        if len(values["ScoreType"]) > 1:
            score_type = f"({','.join(values['ScoreType'])})"
        else:
            score_type = values["ScoreType"][0]

        if len(values["ScoreValue"]) > 1:
            score_value = f"({','.join(map(str, values['ScoreValue']))})"
        else:
            score_value = str(values["ScoreValue"][0])
        
        if len(values["BaselineScoreValue"]) > 1:
            baseline_score_value = f"({','.join(map(str, values['BaselineScoreValue']))})"
        else:
            baseline_score_value = str(values["BaselineScoreValue"][0])

        if len(values["Delta"]) > 1:
            delta = f"({','.join(map(str, [round(delta, 1) for delta in values['Delta']]))})"
        else:
            delta = str(round(values["Delta"][0], 1))

        url = values["Url"][0] if values["Url"] else ""  # Use the first URL if available, or empty string otherwise

        merged_row = [
            values["SourceLang"],
            values["TargetLang"],
            score_type,
            values["TestSet"],
            values["NegativeFlag"],
            score_value,
            baseline_score_value,
            delta,
            url,
        ]
        merged_rows.append(merged_row)

    return merged_rows

def SendEmail():
    logic_app_url = f'{LogicAppUrl}'
    email_body, mismatched_rows = GenerateEmailBody()
    if mismatched_rows:
        mail_subject = f"[{env_type.upper()}] Gender Debias Quality Evaluation Report [Failed]"
    else:
        mail_subject = f"[{env_type.upper()}] Gender Debias Quality Evaluation Report"
    
    data = {
        "mailto": recipients,
        "mailcc": mail_cc,
        "mailsubject": mail_subject,
        "mailbody": email_body,
    }
    response = requests.post(logic_app_url, json=data)

SendEmail()
