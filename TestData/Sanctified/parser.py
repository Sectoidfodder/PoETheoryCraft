import re

datfiles = ["sanc1.txt", "sanc2.txt", "sanc3.txt", "sanc4.txt", "sanc5.txt", "sanc6.txt", "sanc7.txt", "sanc8.txt", "sanc9.txt", "sanc10.txt"]
pattern = re.compile("^\+(\d+) to maximum Life$")
tally = dict()
remainder = dict()
count = 0
for s in datfiles:
	with open(s) as f:
		for line in f:
			if "Astral Plate" in line:
				count += 1
			m = pattern.match(line)
			if m:
				n = int(m.group(1))
				tally[n//10] = tally.get(n//10, 0) + 1
				if n >= 10:
					remainder[n%10] = remainder.get(n%10, 0) + 1
print(count, " items")
print("\nTier")
for n in sorted(tally):
	print(f"{n:02d}", end = " : ")
	for i in range(0, tally[n], 2):
		print("*", end = "")
	print()
print("\nLast digit")
for n in sorted(remainder):
	print(n, end = " : ")
	for i in range(0, remainder[n], 2):
		print("*", end = "")
	print()