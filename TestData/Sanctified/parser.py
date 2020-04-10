import re

datfiles = ['sanc1.txt', 'sanc2.txt']
pattern = re.compile('^\+(\d+) to maximum Life$')
tally = dict()
remainder = dict()
for s in datfiles:
	with open(s) as f:
		for line in f:
			m = pattern.match(line)
			if m:
				n = int(m.group(1))
				tally[n//10] = tally.get(n//10, 0) + 1
				if n >= 10:
					remainder[n%10] = remainder.get(n%10, 0) + 1
print("\nTier")
for n in sorted(tally):
	print(f"{n:02d}", end = " : ")
	for i in range(tally[n]):
		print("*", end = "")
	print()
print("\nLast digit")
for n in sorted(remainder):
	print(n, end = " : ")
	for i in range(remainder[n]):
		print("*", end = "")
	print()