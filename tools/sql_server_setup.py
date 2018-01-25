#! /usr/bin/env python

# For this to work, you will need to install pyodbc
# Also install the ODBC driver, you may need to follow these instructions:
# https://docs.microsoft.com/en-us/sql/connect/odbc/linux-mac/installing-the-microsoft-odbc-driver-for-sql-server

import argparse
import os
from collections import OrderedDict

import pyodbc

# Get current directory name
dir_path = os.path.dirname(os.path.realpath(__file__))


def get_connection_string(args):
    kwargs = vars(args)
    connection_template = ('DRIVER={{ODBC Driver 13 for SQL Server}};' +
                           'PORT=1433;' +
                           'SERVER={server};' +
                           'DATABASE={database};' +
                           'UID={username};' +
                           'PWD={password}')
    connection_string = connection_template.format(**kwargs)
    return connection_string


def execute_file(connection_string, filepath):
    with open(filepath) as f:
        sql_string = f.read()
    sql_string = sql_string.decode("utf-8-sig").encode("utf-8")
    command_list = map(lambda item: item.strip(), sql_string.split('GO'))
    command_list = filter(None, command_list)
    # Connect
    cnxn = pyodbc.connect(connection_string)
    cursor = cnxn.cursor()
    for command in command_list:
        print(command)
        try:
            cursor.execute(command)
            try:
                row = cursor.fetchone()
                while row:
                    print (str(row[0]) + " " + str(row[1]))
                    row = cursor.fetchone()
            except pyodbc.ProgrammingError:
                print('Command provides no results')
        except pyodbc.ProgrammingError as e:
            print('Command failed:')
            print(e)
        cursor.commit()
    cursor.close()


def main(args):
    # Create connection string
    connection_string = get_connection_string(args)

    # Openchain Shemas location
    sql_root = os.path.join(dir_path, './Openchain.SqlServer.Schema')

    # Run the necessary commands to set up the SQL Server on Azure
    sql_files = OrderedDict()
    sql_files['.'] = ['Openchain.sql']
    sql_files['Types'] = ['IdTable.sql', 'RecordMutationTable.sql']
    sql_files['Tables'] = ['Transactions.sql', 'RecordMutations.sql', 'Records.sql']
    sql_files['Stored Procedures'] = [
        'AddTransaction.sql', 'GetAllRecords.sql', 'GetLastTransaction.sql',
        'GetRecordMutations.sql', 'GetRecordRange.sql', 'GetRecords.sql',
        'GetTransaction.sql', 'GetTransactionLog.sql', 'GetTransactionByRecordKeys.sql']

    for path, files in sql_files.items():
        for file in files:
            filepath = os.path.join(sql_root, path, file)
            execute_file(connection_string, filepath)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Setup SQL Server database')
    parser.add_argument('-s', '--server', type=str, required=True,
                        help='Server address for database')
    parser.add_argument('-d', '--database', type=str, required=True,
                        help='Database name')
    parser.add_argument('-u', '--username', type=str, required=True,
                        help='Username for database')
    parser.add_argument('-p', '--password', type=str, required=True,
                        help='Password for database')

    arguments = parser.parse_args()
    main(arguments)
